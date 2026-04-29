using LogSystem.Core.Services.Azure;
using LogSystem.Core.Services.Database;
using LogSystem.Core.Metrics;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using LogSystem.Core.BackgroundServices.Persistence.DefaultMessageReceiver;
using LogSystem.Core.Caching;
using Microsoft.Extensions.Logging;
using LogSystem.Core.Services.Common.Compression;

namespace LogSystem.Core.BackgroundServices.Persistence;

public class LogCollectionBatch(
    PersistenceBackgroundServiceConfig persistenceConfig,
    string logCollectionId,
    LogCollectionCache logCollectionCache,
    Channel<IReceivedMessageModel> messageChannel,
    AzureService azureService,
    DatabaseService databaseService,
    MessagesPerCollectionInTimeWindowReport messagesPerCollectionReport,
    ILogger<BatchPersistenceService> logger,
    CompressionFactory compressionFactory)
{
    private static readonly char[] AvailableRandomizedCharacters = "abcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();

    private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = false,
    };

    private string GenerateRandomizedString(int size)
    {
        char[] result = new char[size];

        for(var i = 0; i < result.Length; i++)
            result[i] = AvailableRandomizedCharacters[Random.Shared.Next(0, AvailableRandomizedCharacters.Length)];

        return new string(result);
    }

    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        DateTime? lastExecution = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            // Wait for messages to arrive
            await messageChannel.Reader.WaitToReadAsync(stoppingToken);

            // Batch tasks
            var tasks = new List<Task>();
            var logCollection = await logCollectionCache.GetOrCreateByClientIdAsync(logCollectionId);
            var batchStartTime = DateTime.UtcNow;

            var batches = await RetrieveBatchesAsync(lastExecution, logCollection.MaxLogsPerFile, stoppingToken);
            
            foreach(var batch in batches)
            {
                var task = ProcessMessagesAsync(logCollection, batchStartTime, batch);
                tasks.Add(task);
            }

            lastExecution = batchStartTime;
            await Task.WhenAll(tasks);
        }
    }

    private async Task<List<List<IReceivedMessageModel>>> RetrieveBatchesAsync(DateTime? lastExecution, int maxLogsForBatch, CancellationToken stoppingToken)
    {
        var result = new List<List<IReceivedMessageModel>>();

        if (stoppingToken.IsCancellationRequested)
            return result;

        // Process all batches with full message in parallel
        while(messageChannel.Reader.Count > maxLogsForBatch)
        {
            var batch = RetrieveMessages(maxLogsForBatch);
            result.Add(batch);
        }

        if(result.Count == 0)
        {
            await ApplyFrequencyDelayAsync(lastExecution, stoppingToken);

            // Check again to process all batches with full message in parallel
            while(messageChannel.Reader.Count > maxLogsForBatch)
            {
                var batch = RetrieveMessages(maxLogsForBatch);
                result.Add(batch);
            }
        }

        // If no full batches were found and delay is satisfied, then get whatever the channel has to offer and process it as a single batch
        if(result.Count == 0)
        {
            var batch = RetrieveMessages(maxLogsForBatch);
            result.Add(batch);
        }

        return result;
    }

    private List<IReceivedMessageModel> RetrieveMessages(int maxLogsForBatch)
    {
        var batch = new List<IReceivedMessageModel>();

        while (batch.Count < maxLogsForBatch && messageChannel.Reader.TryRead(out var message))
            batch.Add(message);

        return batch;
    }

    private async Task ProcessMessagesAsync(LogCollection logCollection, DateTime batchStartTime, List<IReceivedMessageModel> messages)
    {
        var totalStopwatch = Stopwatch.StartNew();

        // Get the default compression strategy (Brotli) and generate filename with appropriate extension
        var compressionStrategy = compressionFactory.GetDefaultStrategy();
        var baseFileName = $"{batchStartTime:yyMMddHHmmss}_{GenerateRandomizedString(6)}.json";
        var persistedFileName = compressionStrategy.AddFormatExtension(baseFileName);

        // Track timing for retrieving log collection
        var retrieveLogCollectionStopwatch = Stopwatch.StartNew();
        var retrieveLogCollectionDuration = retrieveLogCollectionStopwatch.StopAndReturnEllapsed();

        var timingReport = new PersistenceTimingReport
        {
            CollectionClientId = logCollectionId,
            MessageCount = messages.Count,
            RetrieveLogCollection = retrieveLogCollectionDuration
        };

        try
        {
            await PersistBatchAsync(logCollection, messages, persistedFileName, compressionStrategy, timingReport);
            MarkMessagesAsSuccessful(messages);
            timingReport.Success = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist messages for collection {LogCollectionId}", logCollectionId);
            MarkMessagesAsFailed(messages);
            timingReport.Success = false;
        }

        // Handle message acknowledgments
        var ackStopwatch = Stopwatch.StartNew();
        try
        {
            await HandleMessageAcknowledgmentsAsync(messages);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error acknowledging messages for collection {LogCollectionId}", logCollectionId);
        }
        timingReport.AcknowledgeMessages = ackStopwatch.StopAndReturnEllapsed();

        timingReport.TotalExecutionTime = totalStopwatch.StopAndReturnEllapsed();

        // Log the timing report
        logger.LogDebug("{TimingReport}", timingReport.ToFormattedString());

        // Record metrics
        var successCount = timingReport.Success ? timingReport.MessageCount : 0;
        var failedCount = timingReport.Success ? 0 : timingReport.MessageCount;
        messagesPerCollectionReport.RecordBatchPersistence(
            logCollectionId,
            successCount,
            failedCount);

        // Dispose messages
        foreach (var message in messages)
            message.Dispose();
    }

    private async Task ApplyFrequencyDelayAsync(DateTime? lastExecution, CancellationToken stoppingToken)
    {
        if (lastExecution != null)
        {
            var timeSinceLastExecution = DateTime.UtcNow - lastExecution.Value;
            var remainingDelay = persistenceConfig.BatchFillMaxWaitTime - timeSinceLastExecution;

            if (remainingDelay > TimeSpan.Zero)
                await Task.Delay(remainingDelay, stoppingToken);
        }
    }

    private async Task PersistBatchAsync(
        LogCollection logCollection,
        List<IReceivedMessageModel> messages,
        string persistedFileName,
        ICompressionStrategy compressionStrategy,
        PersistenceTimingReport timingReport)
    {
        // Extract logs from messages and update file information
        var updateStopwatch = Stopwatch.StartNew();
        var logs = new List<Log>();
        for (int i = 0; i < messages.Count; i++)
        {
            var message = messages[i];
            var log = message.GetLog();

            // Update source file information for batch persistence
            log.SourceFileIndex = i;
            log.SourceFileName = persistedFileName;
            logs.Add(log);
        }

        timingReport.UpdateLogForFileData = updateStopwatch.StopAndReturnEllapsed();

        // Create JSON content
        var createJsonStopwatch = Stopwatch.StartNew();
        var jsonContent = CreateJsonContent(messages);
        timingReport.Azure.CreateJsonContent = createJsonStopwatch.StopAndReturnEllapsed();

        // Upload to Azure and save to database in parallel
        var azureTask = azureService.UploadFileAsync(
            collectionName: logCollection.TableName,
            fileName: persistedFileName,
            content: jsonContent,
            compressionStrategy: compressionStrategy,
            azureReport: timingReport.Azure);

        var databaseTask = databaseService.SaveLogsAsync(logCollection, logs, timingReport.Database);

        await Task.WhenAll(azureTask, databaseTask);
    }

    private static string CreateJsonContent(List<IReceivedMessageModel> messages)
    {
        var jsonArray = messages.Select(m => m.GetPayloadAsString()).ToList();
        return JsonSerializer.Serialize(jsonArray, JsonSerializerOptions);
    }

    private static void MarkMessagesAsSuccessful(List<IReceivedMessageModel> messages)
    {
        foreach (var msg in messages)
            msg.Status = IReceivedMessageModel.PersistenceStatus.Persisted;
    }

    private static void MarkMessagesAsFailed(List<IReceivedMessageModel> messages)
    {
        foreach (var msg in messages)
            msg.Status = IReceivedMessageModel.PersistenceStatus.Failed;
    }

    private static async Task HandleMessageAcknowledgmentsAsync(List<IReceivedMessageModel> messages)
    {
        foreach (var msg in messages)
        {
            // Fixed: Ack for successful persistence, Nack for failures
            if (msg.Status == IReceivedMessageModel.PersistenceStatus.Persisted)
                await msg.GetRabbitChannel().BasicAckAsync(deliveryTag: msg.GetRabbitMqDeliveryTag(), multiple: false);
            else if (msg.Status == IReceivedMessageModel.PersistenceStatus.Failed)
                await msg.GetRabbitChannel().BasicNackAsync(deliveryTag: msg.GetRabbitMqDeliveryTag(), multiple: false, requeue: false);
            else
                await msg.GetRabbitChannel().BasicNackAsync(deliveryTag: msg.GetRabbitMqDeliveryTag(), multiple: false, requeue: true);
        }
    }
}
