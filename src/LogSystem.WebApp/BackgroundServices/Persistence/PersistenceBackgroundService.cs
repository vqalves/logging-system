
using LogSystem.Core.Services.Database;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading.Channels;

namespace LogSystem.WebApp.BackgroundServices.Persistence;

public class PersistenceBackgroundService(
    PersistenceBackgroundServiceConfig persistenceConfig,
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
                Uri = new Uri(persistenceConfig.RabbitMqConnectionString)
            };

            // Create connection and channel
            await using var rabbitConnection = await factory.CreateConnectionAsync();
            await using var rabbitChannel = await rabbitConnection.CreateChannelAsync();

            // Create async consumer
            var consumer = new AsyncEventingBasicConsumer(rabbitChannel);

            consumer.ReceivedAsync += async (model, ea) =>
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

                    var logCollectionName = receivedMessage.GetLogCollectionClientId();

                    var logCollection = await logCollectionCache.GetOrCreateByClientIdAsync(logCollectionName);
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
                    await rabbitChannel.BasicNackAsync(
                        deliveryTag: ea.DeliveryTag,
                        multiple: false,
                        requeue: false);
                }
            };

            // Start consuming
            await rabbitChannel.BasicConsumeAsync(
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
}