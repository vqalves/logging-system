
using LogSystem.Core.Metrics;
using LogSystem.Core.Services.Database;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using System.Text;
using System.Threading.Channels;

namespace LogSystem.WebApp.BackgroundServices.Persistence.DefaultMessageReceiver;

public class MessageReceiverService(
    PersistenceBackgroundServiceConfig persistenceConfig,
    Channel<IReceivedMessageModel> messageChannel,
    LogCollectionCache logCollectionCache,
    LogAttributeCache logAttributeCache,
    ILogger<MessageReceiverService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MessageReceiverService starting with {ChannelCount} channels...", persistenceConfig.RabbitMqChannelCount);

        var messageReceivedReport = new MessageReceivedReport(logger);

        IConnection? rabbitConnection = null;
        var rabbitChannels = new List<IChannel>();
        var consumerTasks = new List<Task>();

        try
        {
            // Create connection factory
            var factory = new ConnectionFactory
            {
                Uri = new Uri(persistenceConfig.RabbitMqConnectionString)
            };

            // Create connection
            rabbitConnection = await factory.CreateConnectionAsync();

            // Create multiple channels with consumers
            for (int i = 0; i < persistenceConfig.RabbitMqChannelCount; i++)
            {
                var channelIndex = i;
                var channelId = $"Channel-{i + 1}";
                try
                {
                    var rabbitChannel = await rabbitConnection.CreateChannelAsync();
                    rabbitChannels.Add(rabbitChannel);

                    // Set prefetch count for this channel
                    await rabbitChannel.BasicQosAsync(
                        prefetchSize: 0,
                        prefetchCount: persistenceConfig.RabbitMqPrefetchCount,
                        global: false);

                    // Create async consumer
                    var consumer = new AsyncEventingBasicConsumer(rabbitChannel);

                    consumer.ReceivedAsync += async (model, ea) =>
                    {
                        var stopwatch = Stopwatch.StartNew();
                        bool success = false;

                        try
                        {
                            var body = ea.Body.ToArray();
                            var payload = Encoding.UTF8.GetString(body);

                            var receivedMessage = new DefaultReceivedMessageModel
                            {
                                Channel = rabbitChannel,
                                DeliveryTag = ea.DeliveryTag,
                                Payload = payload,
                                Status = IReceivedMessageModel.PersistenceStatus.Pending
                            };

                            var logCollectionName = receivedMessage.GetLogCollectionClientId();

                            var logCollection = await logCollectionCache.GetOrCreateByClientIdAsync(logCollectionName);
                            var attributes = await logAttributeCache.ListAttributesAsync(logCollection);
                            var attributesList = attributes.ToList();

                            var extractionService = new LogExtractionService();
                            receivedMessage.Log = extractionService.Extract(
                                logCollection: logCollection,
                                attributes: attributesList,
                                contentAsJsonDocument: receivedMessage.GetPayloadAsJsonDocument);

                            await messageChannel.Writer.WriteAsync(receivedMessage, stoppingToken);
                            success = true;
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error receiving message on channel {ChannelIndex}", channelIndex);
                            success = false;
                        }
                        finally
                        {
                            messageReceivedReport.RecordMessage(channelId, stopwatch.StopAndReturnEllapsed(), success);
                        }

                        if (!success)
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
                        autoAck: false,
                        consumer: consumer,
                        cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to create channel {ChannelIndex}", channelIndex);
                }
            }

            if (rabbitChannels.Count == 0)
                throw new InvalidOperationException("Failed to create any RabbitMQ channels");

            logger.LogInformation("Successfully started {Count} out of {Total} channels", rabbitChannels.Count, persistenceConfig.RabbitMqChannelCount);

            // Start background reporting task
            await StartRecordingReportAsync(messageReceivedReport, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("MessageReceiverService is stopping due to cancellation");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error in MessageReceiverService");
            throw;
        }
        finally
        {
            // Signal that no more messages will be written
            messageChannel.Writer.Complete();

            // Dispose all channels
            foreach (var channel in rabbitChannels)
            {
                try
                {
                    await channel.CloseAsync();
                    await channel.DisposeAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error disposing RabbitMQ channel");
                }
            }

            // Dispose connection
            if (rabbitConnection != null)
            {
                try
                {
                    await rabbitConnection.CloseAsync();
                    await rabbitConnection.DisposeAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error disposing RabbitMQ connection");
                }
            }

            logger.LogInformation("MessageReceiverService stopped");
        }
    }

    private async Task StartRecordingReportAsync(MessageReceivedReport messageReceivedReport, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                messageReceivedReport.WriteReportIfNeeded();
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error writing message received report");
            }
        }
    }
}