namespace LogSystem.Core.Services.Azure;

public record AzureConfig
{
    /// <summary>
    /// Azure Blob Storage connection string.
    /// </summary>
    public required string ConnectionString { get; init; }

    /// <summary>
    /// Container name for storing log files.
    /// Default value in environment variables: "logs"
    /// </summary>
    public required string ContainerName { get; init; }
}
