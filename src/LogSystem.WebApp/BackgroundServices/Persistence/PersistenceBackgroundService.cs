
using LogSystem.Core.Services.Database;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading.Channels;

namespace LogSystem.WebApp.BackgroundServices.Persistence;

public class PersistenceBackgroundService(
    PersistenceBackgroundServiceConfig persistenceConfig,
    LogSystemConfig logSystemConfig,
    DatabaseService databaseService,
    BatchPersistenceService batchPersistenceService,
    LogCollectionCache logCollectionCache,
    LogAttributeCache logAttributeCache,
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

        try
        {
            // Create connection factory
            var factory = new ConnectionFactory
            {
                Uri = new Uri(persistenceConfig.RabbitMqConnectionString),
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

                    var logCollection = await GetOrCreateLogCollectionAsync(logCollectionName, logCollectionCache);
                    var attributes = await logAttributeCache.ListAttributesAsync(logCollection);
                    var attributesList = attributes.ToList();

                    var extractionService = new LogExtractionService();
                    receivedMessage.Log = extractionService.Extract(
                        logCollection: logCollection,
                        attributes: attributesList,
                        contentAsJsonDocument: receivedMessage.GetJsonDocument);

                    await messageChannel.Writer.WriteAsync(receivedMessage, stoppingToken);
                    success = true;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error receiving message");
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
                queue: persistenceConfig.RabbitMqQueueName,
                autoAck: false, // Manual acknowledgment
                consumer: consumer);

            await batchPersistenceService.ProcessMessagesAsync(messageChannel, logCollectionCache, stoppingToken);
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

    private async Task<LogCollection> GetOrCreateLogCollectionAsync(
        string collectionName,
        LogCollectionCache logCollectionCache)
    {
        var logCollection = await logCollectionCache.GetByClientIdAsync(collectionName, async () =>
        {
            // Create new LogCollection
            var newLogCollection = new LogCollection(
                name: collectionName,
                clientId: collectionName,
                tableName: $"Logs_{collectionName}",
                logDurationHours: logSystemConfig.DefaultLogDurationHours);

            // Save to database
            await databaseService.SaveLogCollectionAsync(newLogCollection);

            // Create default attributes for the new collection
            await CreateLogCollectionAttributesAsync(newLogCollection);

            return newLogCollection;
        });

        return logCollection;
    }

    private async Task CreateLogCollectionAttributesAsync(LogCollection logCollection)
    {
        // Timestamp
        var timestampAttribute = new LogAttribute(
            logCollectionID: logCollection.ID,
            name: "Timestamp",
            sqlColumnName: "Timestamp",
            attributeTypeID: AttributeType.DateTime.Value,
            extractionStyleID: ExtractionStyle.JSON.Value,
            extractionExpression: "$.Timestamp");

        await databaseService.CreateAttributeAsync(logCollection, timestampAttribute);

        var logLevelAttribute = new LogAttribute(
            logCollectionID: logCollection.ID,
            name: "Log Level",
            sqlColumnName: "LogLevel",
            attributeTypeID: AttributeType.Text.Value,
            extractionStyleID: ExtractionStyle.JSON.Value,
            extractionExpression: "$.Level");

        await databaseService.CreateAttributeAsync(logCollection, logLevelAttribute);

        var exceptionAttribute = new LogAttribute(
            logCollectionID: logCollection.ID,
            name: "Exception",
            sqlColumnName: "Exception",
            attributeTypeID: AttributeType.Text.Value,
            extractionStyleID: ExtractionStyle.JSON.Value,
            extractionExpression: "$.Exception.Message");

        await databaseService.CreateAttributeAsync(logCollection, exceptionAttribute);
    }
}