
using LogSystem.Core.Services.Azure;
using LogSystem.Core.Services.Database;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace LogSystem.WebApp.BackgroundServices.Persistence;

public class PersistenceBackgroundService(
    PersistenceBackgroundServiceConfig config,
    AzureService azureService,
    DatabaseService databaseService,
    ILogger<PersistenceBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PersistenceBackgroundService starting...");

        var messageChannel = Channel.CreateUnbounded<ReceivedMessageModel>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        // Initialize caches
        var logCollectionCache = new LogCollectionCache(config.CacheDuration, databaseService);
        var logAttributeCache = new LogAttributeCache(config.CacheDuration, databaseService);

        try
        {
            // Create connection factory
            var factory = new ConnectionFactory
            {
                Uri = new Uri(config.RabbitMqConnectionString),
                DispatchConsumersAsync = true
            };

            // Create connection and channel
            using var rabbitConnection = factory.CreateConnection();
            using var rabbitChannel = rabbitConnection.CreateModel();

            // Create async consumer
            var consumer = new AsyncEventingBasicConsumer(rabbitChannel);

            consumer.Received += async (model, ea) =>
            {
                bool success = false;
                try
                {
                    var body = ea.Body.ToArray();
                    var payload = Encoding.UTF8.GetString(body);

                    var receivedMessage = new ReceivedMessageModel
                    {
                        Channel = rabbitChannel,
                        DeliveryTag = ea.DeliveryTag,
                        Payload = payload,
                        Status = ReceivedMessageModel.PersistenceStatus.Pending
                    };

                    var logCollectionName = receivedMessage.GetLogCollectionName();
                    var hasLogCollectionName = string.IsNullOrWhiteSpace(logCollectionName);

                    if(hasLogCollectionName)
                    {
                        await messageChannel.Writer.WriteAsync(receivedMessage, stoppingToken);
                        success = true;
                    }
                }
                catch (Exception)
                {
                    success = false;
                }

                if(!success)
                {
                    rabbitChannel.BasicNack(
                        deliveryTag: ea.DeliveryTag,
                        multiple: false,
                        requeue: false);
                }
            };

            // Start consuming
            rabbitChannel.BasicConsume(
                queue: config.RabbitMqQueueName,
                autoAck: false, // Manual acknowledgment
                consumer: consumer);

            await StartPersistingMessagesAsync(messageChannel, logCollectionCache, logAttributeCache, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("PersistenceBackgroundService is stopping due to cancellation");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error in PersistenceBackgroundService");
            throw;
        }
    }

    private async Task StartPersistingMessagesAsync(Channel<ReceivedMessageModel> messageChannel, LogCollectionCache logCollectionCache, LogAttributeCache logAttributeCache, CancellationToken stoppingToken)
    {
        // Integrated persistence loop
        DateTime? lastExecution = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            await messageChannel.Reader.WaitToReadAsync();

            // Implement delay logic based on lastExecution and maxFrequency
            if (lastExecution != null)
            {
                var timeSinceLastExecution = DateTime.UtcNow - lastExecution.Value;
                var remainingDelay = config.MaxFrequency - timeSinceLastExecution;

                if (remainingDelay > TimeSpan.Zero)
                    await Task.Delay(remainingDelay, stoppingToken);
            }

            var batchStartTime = DateTime.UtcNow;

            // Read all available messages (non-blocking)
            var messages = new List<ReceivedMessageModel>();
            while (messageChannel.Reader.TryRead(out var message))
                messages.Add(message);

            // Group messages by collection name
            var messagesPerCollectionName = messages
                .GroupBy(x => x.GetLogCollectionName())
                .Select(x => new { CollectionName = x.Key, Messages = x.ToList() })
                .ToList();

            foreach (var collectionGroup in messagesPerCollectionName)
            {
                try
                {
                    // Get or create LogCollection
                    var logCollection = await logCollectionCache.GetByNameAsync(collectionGroup.CollectionName);

                    if (logCollection == null)
                    {
                        // Create new collection
                        logCollection = new LogCollection(
                            name: collectionGroup.CollectionName,
                            tableName: $"Logs_{collectionGroup.CollectionName}",
                            logDurationHours: config.DefaultLogDurationHours);

                        await databaseService.SaveLogCollectionAsync(logCollection);
                        logCollectionCache.InvalidateCache(logCollection);
                        logCollection = await logCollectionCache.GetByNameAsync(collectionGroup.CollectionName);

                        // TODO: Create an attribute for new LogCollection, named "Timestamp", that will extract from "$.Timestamp" and is integer
                    }

                    // Get attributes for this collection
                    var attributes = await logAttributeCache.ListAttributesAsync(logCollection!);
                    var attributesList = attributes.ToList();

                    // Create JSON array for Azure upload
                    var jsonArray = collectionGroup.Messages.Select(m => m.Payload).ToList();
                    var jsonContent = JsonSerializer.Serialize(jsonArray, new JsonSerializerOptions
                    {
                        WriteIndented = false
                    });

                    var persistedFileName = $"{batchStartTime:yyyyMMddHHmmss}.json";

                    // Extract logs from messages
                    var logs = new List<Log>();
                    var extractionService = new LogExtractionService();

                    // Move the logic to extract the log to be done in the RabbitMQ consumer
                    // If extraction fails, nack the message
                    for (int i = 0; i < collectionGroup.Messages.Count; i++)
                    {
                        try
                        {
                            var log = extractionService.Extract(
                                logCollection: logCollection,
                                attributes: attributesList,
                                logContent: collectionGroup.Messages[i].Payload,
                                sourceFileIndex: i,
                                sourceFileName: persistedFileName);

                            logs.Add(log);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Failed to extract log at index {Index}", i);
                        }
                    }

                    // Execute Azure upload and database insert in parallel
                    var fileDuration = TimeSpan.FromHours(logCollection.LogDurationHours);

                    var azureTask = azureService.UploadFileAsync(
                        logCollectionId: logCollection.ID,
                        fileName: persistedFileName,
                        fileDuration: fileDuration,
                        content: jsonContent);

                    var databaseTask = databaseService.SaveLogsAsync(logCollection, logs);

                    await Task.WhenAll(azureTask, databaseTask);

                    // Mark all messages in this collection as successful
                    foreach (var msg in collectionGroup.Messages)
                        msg.Status = ReceivedMessageModel.PersistenceStatus.Persisted;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to persist messages for collection {Name}", collectionGroup.CollectionName);

                    // Mark all messages in this collection as failed
                    foreach (var msg in collectionGroup.Messages)
                        msg.Status = ReceivedMessageModel.PersistenceStatus.Failed;
                }
            }

            // Bulk ack/nack logic
            // Group messages by channel
            var messagesByChannel = messages.GroupBy(m => m.Channel).ToList();

            foreach (var channelGroup in messagesByChannel)
            {
                var channelMessages = channelGroup.ToList();
                var successfulMessages = channelMessages.Where(m => m.Status == ReceivedMessageModel.PersistenceStatus.Persisted).ToList();
                var failedMessages = channelMessages.Where(m => m.Status == ReceivedMessageModel.PersistenceStatus.Failed).ToList();

                if (successfulMessages.Count == channelMessages.Count)
                {
                    // All messages successful - bulk ack
                    var highestDeliveryTag = channelMessages.Max(m => m.DeliveryTag);
                    channelGroup.Key.BasicAck(deliveryTag: highestDeliveryTag, multiple: true);
                    logger.LogDebug("Bulk acknowledged {Count} messages", channelMessages.Count);
                }
                else if (failedMessages.Count == channelMessages.Count)
                {
                    // All messages failed - bulk nack without requeue
                    var highestDeliveryTag = channelMessages.Max(m => m.DeliveryTag);
                    channelGroup.Key.BasicNack(deliveryTag: highestDeliveryTag, multiple: true, requeue: false);
                    logger.LogDebug("Bulk nack'd {Count} messages", channelMessages.Count);
                }
                else
                {
                    // Mixed results - individual ack/nack
                    foreach (var msg in successfulMessages)
                    {
                        msg.Channel.BasicAck(deliveryTag: msg.DeliveryTag, multiple: false);
                    }

                    foreach (var msg in failedMessages)
                    {
                        msg.Channel.BasicNack(deliveryTag: msg.DeliveryTag, multiple: false, requeue: false);
                    }

                    logger.LogDebug("Individually ack'd {Success} and nack'd {Failed} messages",
                        successfulMessages.Count, failedMessages.Count);
                }
            }

            lastExecution = DateTime.UtcNow;
        }
    }
}