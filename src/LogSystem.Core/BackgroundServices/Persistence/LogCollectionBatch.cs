using LogSystem.Core.Services.Azure;
using LogSystem.Core.Services.Database;
using LogSystem.Core.Metrics;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using LogSystem.Core.BackgroundServices.Persistence.DefaultMessageReceiver;
using LogSystem.Core.Caching;
using Microsoft.Extensions.Logging;

namespace LogSystem.Core.BackgroundServices.Persistence;

public class LogCollectionBatch(
    PersistenceBackgroundServiceConfig persistenceConfig,
    string logCollectionId,
    LogCollectionCache logCollectionCache,
    Channel<IReceivedMessageModel> messageChannel,
    AzureService azureService,
    DatabaseService databaseService,
    MessagesPerCollectionInTimeWindowReport messagesPerCollectionReport,
    ILogger<BatchPersistenceService> logger)
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

            // Retrieve LogCollection from cache for this batch
            var logCollection = await logCollectionCache.GetOrCreateByClientIdAsync(logCollectionId);

            var batchStartTime = DateTime.UtcNow;
            var maxLogsForBatch = logCollection.MaxLogsPerFile;

            // Read messages from channel up to MaxLogsPerFile or until channel is empty
            var readingStopwatch = Stopwatch.StartNew();
            var messages = ReadAvailableMessages(maxLogsForBatch);
            var readingDuration = readingStopwatch.StopAndReturnEllapsed();

            if(messages.Count == 0)
                continue;

            // Apply frequency delay only if not reaching MaxLogsPerFile
            if (messages.Count < maxLogsForBatch)
                await ApplyFrequencyDelayAsync(lastExecution, stoppingToken);

            // Fill batch with remaining messages
            readingStopwatch = Stopwatch.StartNew();
            messages = messages.Concat(ReadAvailableMessages(maxLogsForBatch - messages.Count)).ToList();
            readingDuration += readingStopwatch.StopAndReturnEllapsed();

            lastExecution = batchStartTime;

            var totalStopwatch = Stopwatch.StartNew();

            var persistedFileName = $"{batchStartTime:yyMMddHHmmss}_{GenerateRandomizedString(6)}.json.gzip";

            // Track timing for retrieving log collection
            var retrieveLogCollectionStopwatch = Stopwatch.StartNew();
            logCollection = await logCollectionCache.GetOrCreateByClientIdAsync(logCollectionId);
            var retrieveLogCollectionDuration = retrieveLogCollectionStopwatch.StopAndReturnEllapsed();

            var timingReport = new PersistenceTimingReport
            {
                CollectionClientId = logCollection.ClientId,
                MessageCount = messages.Count,
                ReadFromChannel = readingDuration,
                RetrieveLogCollection = retrieveLogCollectionDuration
            };

            try
            {
                await PersistBatchAsync(logCollection, messages, persistedFileName, timingReport);
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
                logCollection.ClientId,
                successCount,
                failedCount);

            // Dispose messages
            foreach (var message in messages)
                message.Dispose();
        }
    }

    private async Task ApplyFrequencyDelayAsync(DateTime? lastExecution, CancellationToken stoppingToken)
    {
        if (lastExecution != null)
        {
            var timeSinceLastExecution = DateTime.UtcNow - lastExecution.Value;
            var remainingDelay = persistenceConfig.MaxFrequency - timeSinceLastExecution;

            if (remainingDelay > TimeSpan.Zero)
                await Task.Delay(remainingDelay, stoppingToken);
        }
    }

    private List<IReceivedMessageModel> ReadAvailableMessages(int maxMessages)
    {
        var messages = new List<IReceivedMessageModel>();

        while (messages.Count < maxMessages && messageChannel.Reader.TryRead(out var message))
            messages.Add(message);

        return messages;
    }

    private async Task PersistBatchAsync(
        LogCollection logCollection,
        List<IReceivedMessageModel> messages,
        string persistedFileName,
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
