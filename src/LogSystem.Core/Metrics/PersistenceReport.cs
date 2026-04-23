using System.Diagnostics;
using System.Text.Json;

namespace LogSystem.Core.Metrics;

public class PersistenceReport
{
    public TimeSpan ReadingFromChannel { get; set; } = TimeSpan.Zero;
    public TimeSpan GroupingByCollectionName { get; set; } = TimeSpan.Zero;
    public int MessageCount { get; set; } = 0;
    public TimeSpan TotalExecutionTime { get; set; } = TimeSpan.Zero;
    public List<CollectionBatchReport> Batches { get; set; } = new();

    public string ToFormattedString()
    {
        var totalSuccess = Batches.Sum(b => b.SuccessfulMessageCount);
        var totalFailed = Batches.Sum(b => b.FailedMessageCount);
        var collectionCount = Batches.Count;

        var content = JsonSerializer.Serialize(this, new JsonSerializerOptions()
        {
            WriteIndented = false
        });

        return $"Batch persistence completed: {MessageCount} messages ({totalSuccess} success, {totalFailed} failed) across {collectionCount} collections in {TotalExecutionTime.TotalMilliseconds:F2}ms: {content}";
    }
}

public class CollectionBatchReport
{
    public string CollectionClientId { get; set; } = string.Empty;
    public int MessageCount { get; set; } = 0;
    public int SuccessfulMessageCount { get; set; } = 0;
    public int FailedMessageCount { get; set; } = 0;
    public TimeSpan RetrieveLogCollection { get; set; } = TimeSpan.Zero;
    public TimeSpan UpdateLogForFileData { get; set; } = TimeSpan.Zero;
    public TimeSpan TotalExecutionTime { get; set; } = TimeSpan.Zero;
    public AzureOperationReport Azure { get; set; } = new();
    public DatabaseOperationReport Database { get; set; } = new();
}

public class AzureOperationReport
{
    public TimeSpan CreateJsonContent { get; set; } = TimeSpan.Zero;
    public TimeSpan CompressToGzip { get; set; } = TimeSpan.Zero;
    public TimeSpan UploadFile { get; set; } = TimeSpan.Zero;
    public TimeSpan TotalExecutionTime { get; set; } = TimeSpan.Zero;
}

public class DatabaseOperationReport
{
    public TimeSpan OpenConnectionToDatabase { get; set; } = TimeSpan.Zero;
    public TimeSpan SaveData { get; set; } = TimeSpan.Zero;
    public TimeSpan TotalExecutionTime { get; set; } = TimeSpan.Zero;
}
