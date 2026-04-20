using LogSystem.Core.Services.Azure;
using LogSystem.Core.Services.Database;
using RabbitMQ.Client;
using System.Text.Json;
using System.Threading.Channels;

namespace LogSystem.WebApp.BackgroundServices.Persistence;

public class BatchPersistenceService(
    PersistenceBackgroundServiceConfig persistenceConfig,
    LogSystemConfig logSystemConfig,
    AzureService azureService,
    DatabaseService databaseService,
    ILogger<BatchPersistenceService> logger)
{
    public async Task ProcessMessagesAsync(
        Channel<ReceivedMessageModel> messageChannel,
        LogCollectionCache logCollectionCache,
        CancellationToken stoppingToken)
    {
        DateTime? lastExecution = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            await messageChannel.Reader.WaitToReadAsync(stoppingToken);

            await ApplyFrequencyDelayAsync(lastExecution, stoppingToken);

            var batchStartTime = DateTime.UtcNow;
            lastExecution = batchStartTime;

            var messages = ReadAvailableMessages(messageChannel);
            var messagesPerCollectionName = GroupMessagesByCollection(messages);

            await ProcessBatchAsync(messagesPerCollectionName, logCollectionCache, batchStartTime);

            HandleMessageAcknowledgments(messages);
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

    private List<ReceivedMessageModel> ReadAvailableMessages(Channel<ReceivedMessageModel> messageChannel)
    {
        var messages = new List<ReceivedMessageModel>();
        while (messageChannel.Reader.TryRead(out var message))
            messages.Add(message);
        return messages;
    }

    private List<CollectionMessageGroup> GroupMessagesByCollection(List<ReceivedMessageModel> messages)
    {
        return messages
            .GroupBy(x => x.GetLogCollectionName())
            .Select(x => new CollectionMessageGroup(x.Key, x.ToList()))
            .ToList();
    }

    private async Task ProcessBatchAsync(
        List<CollectionMessageGroup> messagesPerCollectionName,
        LogCollectionCache logCollectionCache,
        DateTime batchStartTime)
    {
        foreach (var collectionGroup in messagesPerCollectionName)
        {
            try
            {
                await PersistCollectionGroupAsync(
                    collectionGroup,
                    logCollectionCache,
                    batchStartTime);

                MarkMessagesAsSuccessful(collectionGroup.Messages);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to persist messages for collection {Name}", collectionGroup.CollectionName);
                MarkMessagesAsFailed(collectionGroup.Messages);
            }
        }
    }

    private async Task PersistCollectionGroupAsync(
        CollectionMessageGroup collectionGroup,
        LogCollectionCache logCollectionCache,
        DateTime batchStartTime)
    {
        var logCollection = await GetOrCreateLogCollectionAsync(collectionGroup.CollectionName, logCollectionCache);

        var persistedFileName = $"{batchStartTime:yyyyMMddHHmmss}.json";

        // Extract logs from messages (using pre-extracted logs where available)
        var logs = new List<Log>();
        for (int i = 0; i < collectionGroup.Messages.Count; i++)
        {
            var message = collectionGroup.Messages[i];
            if (message.Log != null)
            {
                // Update source file information for batch persistence
                message.Log.SourceFileIndex = i;
                message.Log.SourceFileName = persistedFileName;
                logs.Add(message.Log);
            }
        }

        await PersistLogsAsync(collectionGroup, logCollection, logs, persistedFileName);
    }

    private async Task<LogCollection> GetOrCreateLogCollectionAsync(
        string collectionName,
        LogCollectionCache logCollectionCache)
    {
        var logCollection = await logCollectionCache.GetByNameAsync(collectionName);

        if (logCollection == null)
        {
            logCollection = new LogCollection(
                name: collectionName,
                tableName: $"Logs_{collectionName}",
                logDurationHours: logSystemConfig.DefaultLogDurationHours);

            await databaseService.SaveLogCollectionAsync(logCollection);
            logCollectionCache.InvalidateCache(logCollection);
            logCollection = await logCollectionCache.GetByNameAsync(collectionName);

            await CreateTimestampAttributeAsync(logCollection!);
        }

        return logCollection!;
    }

    private async Task CreateTimestampAttributeAsync(LogCollection logCollection)
    {
        var timestampAttribute = new LogAttribute(
            logCollectionID: logCollection.ID,
            name: "Timestamp",
            sqlColumnName: "Timestamp",
            attributeTypeID: AttributeType.DateTime.Value,
            extractionStyleID: ExtractionStyle.JSON.Value,
            extractionExpression: "$.Timestamp");

        await databaseService.CreateAttributeAsync(logCollection, timestampAttribute);
    }

    private async Task PersistLogsAsync(
        CollectionMessageGroup collectionGroup,
        LogCollection logCollection,
        List<Log> logs,
        string persistedFileName)
    {
        var jsonContent = CreateJsonContent(collectionGroup.Messages);
        var fileDuration = TimeSpan.FromHours(logCollection.LogDurationHours);

        var azureTask = azureService.UploadFileAsync(
            logCollectionId: logCollection.ID,
            fileName: persistedFileName,
            fileDuration: fileDuration,
            content: jsonContent);

        var databaseTask = databaseService.SaveLogsAsync(logCollection, logs);

        await Task.WhenAll(azureTask, databaseTask);
    }

    private string CreateJsonContent(List<ReceivedMessageModel> messages)
    {
        var jsonArray = messages.Select(m => m.Payload).ToList();
        return JsonSerializer.Serialize(jsonArray, new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }

    private void MarkMessagesAsSuccessful(List<ReceivedMessageModel> messages)
    {
        foreach (var msg in messages)
            msg.Status = ReceivedMessageModel.PersistenceStatus.Persisted;
    }

    private void MarkMessagesAsFailed(List<ReceivedMessageModel> messages)
    {
        foreach (var msg in messages)
            msg.Status = ReceivedMessageModel.PersistenceStatus.Failed;
    }

    private void HandleMessageAcknowledgments(List<ReceivedMessageModel> messages)
    {
        var messagesByChannel = messages.GroupBy(m => m.Channel).ToList();

        foreach (var channelGroup in messagesByChannel)
        {
            var channelMessages = channelGroup.ToList();
            var successfulMessages = channelMessages.Where(m => m.Status == ReceivedMessageModel.PersistenceStatus.Persisted).ToList();
            var failedMessages = channelMessages.Where(m => m.Status == ReceivedMessageModel.PersistenceStatus.Failed).ToList();

            if (successfulMessages.Count == channelMessages.Count)
            {
                var highestDeliveryTag = messages.Max(m => m.DeliveryTag);
                channelGroup.Key.BasicAck(deliveryTag: highestDeliveryTag, multiple: true);
            }
            else if (failedMessages.Count == channelMessages.Count)
            {
                var highestDeliveryTag = messages.Max(m => m.DeliveryTag);
                channelGroup.Key.BasicNack(deliveryTag: highestDeliveryTag, multiple: true, requeue: false);
            }
            else
            {
                foreach (var msg in successfulMessages)
                    msg.Channel.BasicAck(deliveryTag: msg.DeliveryTag, multiple: false);

                foreach (var msg in failedMessages)
                    msg.Channel.BasicNack(deliveryTag: msg.DeliveryTag, multiple: false, requeue: false);
            }
        }
    }
    private record CollectionMessageGroup(string CollectionName, List<ReceivedMessageModel> Messages);
}
