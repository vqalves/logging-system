using LogSystem.Core.Services.Azure;
using LogSystem.Core.Services.Database;
using LogSystem.Core.Metrics;
using System.Threading.Channels;
using LogSystem.Core.BackgroundServices.Persistence.DefaultMessageReceiver;
using LogSystem.Core.Caching;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using LogSystem.Core.Services.Common.Compression;

namespace LogSystem.Core.BackgroundServices.Persistence;

public class BatchPersistenceService(
    PersistenceBackgroundServiceConfig persistenceConfig,
    Channel<IReceivedMessageModel> messageChannel,
    LogCollectionCache logCollectionCache,
    AzureService azureService,
    DatabaseService databaseService,
    MessagesPerCollectionInTimeWindowReport messagesPerCollectionReport,
    ILogger<BatchPersistenceService> logger,
    CompressionFactory compressionFactory) : BackgroundService
{
    private readonly Dictionary<string, LogCollectionBatchInfo> _batches = new();
    private readonly List<Task> _batchTasks = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("BatchPersistenceService started");

        try
        {
            // Read from messageChannel until cancelled
            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait for messages to arrive
                await messageChannel.Reader.WaitToReadAsync(stoppingToken);

                // Read a message
                while(messageChannel.Reader.TryRead(out var message))
                {

                    // Get the LogCollectionClientID from the message
                    var logCollectionClientId = message.GetLogCollectionClientId();
                    var batchInfo = CreateLogCollectionBatch(logCollectionClientId, stoppingToken);

                    // Publish the message to the batch's channel
                    await batchInfo.Channel.Writer.WriteAsync(message, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("BatchPersistenceService stopping - cancellation requested");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error in BatchPersistenceService");
        }
        finally
        {
            // Complete all batch channels to signal no more messages
            foreach (var batchInfo in _batches.Values)
                batchInfo.Channel.Writer.Complete();

            // Wait for all batch tasks to complete
            logger.LogInformation("Waiting for {BatchCount} batch tasks to complete", _batchTasks.Count);
            await Task.WhenAll(_batchTasks);

            logger.LogInformation("BatchPersistenceService stopped");
        }
    }

    private LogCollectionBatchInfo CreateLogCollectionBatch(string logCollectionClientId, CancellationToken stoppingToken)
    {
        // Lookup or create LogCollectionBatch for this client ID
        if (!_batches.TryGetValue(logCollectionClientId, out var batchInfo))
        {
            // Create new LogCollectionBatch and its channel
            var batchChannel = Channel.CreateUnbounded<IReceivedMessageModel>();

            var batch = new LogCollectionBatch(
                persistenceConfig,
                logCollectionClientId,
                logCollectionCache,
                batchChannel,
                azureService,
                databaseService,
                messagesPerCollectionReport,
                logger,
                compressionFactory);

            // Start the batch processing task
            var batchTask = batch.ExecuteAsync(stoppingToken);

            batchInfo = new LogCollectionBatchInfo
            {
                Batch = batch,
                Channel = batchChannel,
                Task = batchTask
            };

            _batches[logCollectionClientId] = batchInfo;
            _batchTasks.Add(batchTask);

            logger.LogInformation("Created new LogCollectionBatch for collection {LogCollectionClientId}", logCollectionClientId);
        }

        return batchInfo;
    }

    private class LogCollectionBatchInfo
    {
        public required LogCollectionBatch Batch { get; init; }
        public required Channel<IReceivedMessageModel> Channel { get; init; }
        public required Task Task { get; init; }
    }
}
