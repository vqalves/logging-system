using System.Text.Json;

namespace LogSystem.Core.Metrics;

public class PersistenceTimingReport
{
    public required string CollectionClientId { get; set; }
    public int MessageCount { get; set; }
    public bool Success { get; set; }
    public TimeSpan RetrieveLogCollection { get; set; }
    public TimeSpan UpdateLogForFileData { get; set; }
    public TimeSpan AcknowledgeMessages { get; set; }
    public TimeSpan TotalExecutionTime { get; set; }
    public AzureOperationReport Azure { get; set; } = new();
    public DatabaseOperationReport Database { get; set; } = new();

    private static JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
    {
        WriteIndented = false
    };

    public string ToFormattedString()
    {
        return JsonSerializer.Serialize(this, JsonOptions);
    }
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