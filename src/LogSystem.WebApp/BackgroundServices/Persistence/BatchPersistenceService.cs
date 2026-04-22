using LogSystem.Core.Services.Azure;
using LogSystem.Core.Services.Database;
using System.Text.Json;
using System.Threading.Channels;

namespace LogSystem.WebApp.BackgroundServices.Persistence;

public class BatchPersistenceService(
    PersistenceBackgroundServiceConfig persistenceConfig,
    AzureService azureService,
    DatabaseService databaseService,
    ILogger<BatchPersistenceService> logger)
{
    private readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = false,
    };

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

            var persistedFileName = $"{batchStartTime:yyyyMMddHHmmss}.json.gzip";

            try
            {
                await ProcessBatchesAsync(messagesPerCollectionName, logCollectionCache, persistedFileName);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Error persisting batch");
            }

            try
            {
                await HandleMessageAcknowledgmentsAsync(messages);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Error acknowleding messages");
            }
            
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
            .GroupBy(x => x.GetLogCollectionClientId())
            .Select(x => new CollectionMessageGroup(x.Key, x.ToList()))
            .ToList();
    }

    private async Task ProcessBatchesAsync(
        List<CollectionMessageGroup> messagesPerCollectionName,
        LogCollectionCache logCollectionCache,
        string persistedFileName)
    {
        var createdTasks = new List<Task>(messagesPerCollectionName.Count);
        
        foreach (var collectionGroup in messagesPerCollectionName)
        {
            var task = Task.Run(() => ProcessBatchAsync(logCollectionCache, persistedFileName, collectionGroup));
            createdTasks.Add(task);
        }

        await Task.WhenAll(createdTasks);
    }

    private async Task ProcessBatchAsync(LogCollectionCache logCollectionCache, string persistedFileName, CollectionMessageGroup collectionGroup)
    {
        try
        {
            await PersistCollectionGroupAsync(
                collectionGroup,
                logCollectionCache,
                persistedFileName);

            MarkMessagesAsSuccessful(collectionGroup.Messages);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist messages for collection {Name}", collectionGroup.CollectionClientId);
            MarkMessagesAsFailed(collectionGroup.Messages);
        }
    }

    private async Task PersistCollectionGroupAsync(
        CollectionMessageGroup collectionGroup,
        LogCollectionCache logCollectionCache,
        string persistedFileName)
    {
        var logCollection = await logCollectionCache.GetOrCreateByClientIdAsync(collectionGroup.CollectionClientId);

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

    private async Task PersistLogsAsync(
        CollectionMessageGroup collectionGroup,
        LogCollection logCollection,
        List<Log> logs,
        string persistedFileName)
    {
        var jsonContent = CreateJsonContent(collectionGroup.Messages);

        var azureTask = azureService.UploadFileAsync(
            collectionName: logCollection.TableName,
            fileName: persistedFileName,
            content: jsonContent);

        var databaseTask = databaseService.SaveLogsAsync(logCollection, logs);

        await Task.WhenAll(azureTask, databaseTask);
    }

    private string CreateJsonContent(List<ReceivedMessageModel> messages)
    {
        var jsonArray = messages.Select(m => m.Payload).ToList();

        return JsonSerializer.Serialize(jsonArray, JsonSerializerOptions);
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

    private async Task HandleMessageAcknowledgmentsAsync(List<ReceivedMessageModel> messages)
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
                await channelGroup.Key.BasicAckAsync(deliveryTag: highestDeliveryTag, multiple: true);
            }
            else if (failedMessages.Count == channelMessages.Count)
            {
                var highestDeliveryTag = messages.Max(m => m.DeliveryTag);
                await channelGroup.Key.BasicNackAsync(deliveryTag: highestDeliveryTag, multiple: true, requeue: false);
            }
            else
            {
                foreach (var msg in successfulMessages)
                    await msg.Channel.BasicAckAsync(deliveryTag: msg.DeliveryTag, multiple: false);

                foreach (var msg in failedMessages)
                    await msg.Channel.BasicNackAsync(deliveryTag: msg.DeliveryTag, multiple: false, requeue: false);
            }
        }
    }
    private record CollectionMessageGroup(string CollectionClientId, List<ReceivedMessageModel> Messages);
}
