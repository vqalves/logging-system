namespace LogSystem.Core.Metrics;

public class MessagesPerCollectionInTimeWindowReport
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
    }

    public void RecordBatchPersistence(
        string collectionClientId,
        int successCount,
        int failedCount)
    {
        lock (_lock)
        {
            CleanOldRecords();

            _records.Add(new CollectionPersistenceRecord
            {
                CollectionClientId = collectionClientId,
                SuccessCount = successCount,
                FailedCount = failedCount,
                Timestamp = DateTime.UtcNow
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
                        FailedCount = g.Sum(r => r.FailedCount)
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
    }
}
