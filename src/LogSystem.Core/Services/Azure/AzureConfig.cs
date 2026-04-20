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

    /// <summary>
    /// Azure Subscription ID for managing storage account lifecycle policies.
    /// </summary>
    public required string SubscriptionId { get; init; }

    /// <summary>
    /// Resource Group name where the storage account is located.
    /// </summary>
    public required string ResourceGroupName { get; init; }

    /// <summary>
    /// Storage Account name for managing lifecycle policies.
    /// </summary>
    public required string StorageAccountName { get; init; }

    /// <summary>
    /// Service Principal Tenant ID for Azure authentication.
    /// </summary>
    public required string TenantId { get; init; }

    /// <summary>
    /// Service Principal Client ID for Azure authentication.
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// Service Principal Client Secret for Azure authentication.
    /// </summary>
    public required string ClientSecret { get; init; }
}
