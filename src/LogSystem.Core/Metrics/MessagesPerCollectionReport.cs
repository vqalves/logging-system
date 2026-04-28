namespace LogSystem.Core.Metrics;

public class MessagesPerCollectionReport
{
    private readonly object _lock = new();
    private readonly List<CollectionPersistenceRecord> _records = [];
    private readonly TimeSpan _retentionWindow = TimeSpan.FromSeconds(10);

    private class CollectionPersistenceRecord
    {
        public required string CollectionClientId { get; init; }
        public required int SuccessCount { get; init; }
        public required int FailedCount { get; init; }
        public required DateTime Timestamp { get; init; }
        public required TimeSpan ReadingPayload { get; init; }
        public required TimeSpan ExtratingCollectionName { get; init; }
        public required TimeSpan ExtratingLog { get; init; }
        public required TimeSpan WritingToChannel { get; init; }
    }

    public void RecordBatchPersistence(
        string collectionClientId,
        int successCount,
        int failedCount,
        TimeSpan readingPayload = default,
        TimeSpan extratingCollectionName = default,
        TimeSpan extratingLog = default,
        TimeSpan writingToChannel = default)
    {
        lock (_lock)
        {
            CleanOldRecords();

            _records.Add(new CollectionPersistenceRecord
            {
                CollectionClientId = collectionClientId,
                SuccessCount = successCount,
                FailedCount = failedCount,
                Timestamp = DateTime.UtcNow,
                ReadingPayload = readingPayload,
                ExtratingCollectionName = extratingCollectionName,
                ExtratingLog = extratingLog,
                WritingToChannel = writingToChannel
            });
        }
    }

    public Dictionary<string, CollectionStats> GetCurrentStats()
    {
        lock (_lock)
        {
            CleanOldRecords();

            return _records
                .GroupBy(r => r.CollectionClientId)
                .ToDictionary(
                    g => g.Key,
                    g => new CollectionStats
                    {
                        SuccessCount = g.Sum(r => r.SuccessCount),
                        FailedCount = g.Sum(r => r.FailedCount),
                        AverageReadingPayload = g.Any() ? TimeSpan.FromTicks((long)g.Average(r => r.ReadingPayload.Ticks)) : TimeSpan.Zero,
                        AverageExtratingCollectionName = g.Any() ? TimeSpan.FromTicks((long)g.Average(r => r.ExtratingCollectionName.Ticks)) : TimeSpan.Zero,
                        AverageExtratingLog = g.Any() ? TimeSpan.FromTicks((long)g.Average(r => r.ExtratingLog.Ticks)) : TimeSpan.Zero,
                        AverageWritingToChannel = g.Any() ? TimeSpan.FromTicks((long)g.Average(r => r.WritingToChannel.Ticks)) : TimeSpan.Zero
                    });
        }
    }

    private void CleanOldRecords()
    {
        var cutoffTime = DateTime.UtcNow - _retentionWindow;
        _records.RemoveAll(r => r.Timestamp < cutoffTime);
    }

    public class CollectionStats
    {
        public int SuccessCount { get; init; }
        public int FailedCount { get; init; }
        public int TotalCount => SuccessCount + FailedCount;
        public TimeSpan AverageReadingPayload { get; init; }
        public TimeSpan AverageExtratingCollectionName { get; init; }
        public TimeSpan AverageExtratingLog { get; init; }
        public TimeSpan AverageWritingToChannel { get; init; }
    }
}
