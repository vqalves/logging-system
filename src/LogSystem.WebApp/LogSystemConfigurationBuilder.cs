
using LogSystem.Core.Services.Azure;
using LogSystem.Core.Services.Database;
using LogSystem.WebApp.BackgroundServices.Persistence;
using LogSystem.WebApp.BackgroundServices.Cleanup;
using LogSystem.Core.Services.RabbitMq;
using LogSystem.Core.Caching;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LogSystem.WebApp;

public class LogSystemConfigurationBuilder
{
    public Dictionary<string, string> GlobalConfig = new();
    public Dictionary<string, string>? LocalConfig;

    public LogSystemConfigurationBuilder()
    {
        string? localConfigPath = Environment.GetEnvironmentVariable("LOCAL_ENVIRONMENT_VARIABLES_PATH");

        if (!string.IsNullOrEmpty(localConfigPath) && File.Exists(localConfigPath))
        {
            var localJson = File.ReadAllText(localConfigPath);
            localJson = Regex.Replace(localJson, @"\n\s+//[^\n]*", string.Empty, RegexOptions.Compiled);

            LocalConfig = JsonSerializer.Deserialize<Dictionary<string, string>>(localJson);
        }
    }

    public AzureConfig GetAzureConfig()
    {
        var connectionString = GetConfigValue("AZURE_BLOB_STORAGE_CONNECTION_STRING") ?? throw new InvalidOperationException("AZURE_BLOB_STORAGE_CONNECTION_STRING not found in configuration");
        var containerName = GetConfigValue("AZURE_BLOB_STORAGE_CONTAINER_NAME") ?? "logs";
        var subscriptionId = GetConfigValue("AZURE_SUBSCRIPTION_ID") ?? throw new InvalidOperationException("AZURE_SUBSCRIPTION_ID not found in configuration");
        var resourceGroupName = GetConfigValue("AZURE_RESOURCE_GROUP_NAME") ?? throw new InvalidOperationException("AZURE_RESOURCE_GROUP_NAME not found in configuration");
        var storageAccountName = GetConfigValue("AZURE_STORAGE_ACCOUNT_NAME") ?? throw new InvalidOperationException("AZURE_STORAGE_ACCOUNT_NAME not found in configuration");
        var tenantId = GetConfigValue("AZURE_TENANT_ID") ?? throw new InvalidOperationException("AZURE_TENANT_ID not found in configuration");
        var clientId = GetConfigValue("AZURE_CLIENT_ID") ?? throw new InvalidOperationException("AZURE_CLIENT_ID not found in configuration");
        var clientSecret = GetConfigValue("AZURE_CLIENT_SECRET") ?? throw new InvalidOperationException("AZURE_CLIENT_SECRET not found in configuration");

        return new AzureConfig
        {
            ConnectionString = connectionString,
            ContainerName = containerName,
            SubscriptionId = subscriptionId,
            ResourceGroupName = resourceGroupName,
            StorageAccountName = storageAccountName,
            TenantId = tenantId,
            ClientId = clientId,
            ClientSecret = clientSecret
        };
    }

    public DatabaseConfig GetDatabaseConfig()
    {
        var connectionString = GetConfigValue("LOG_DATABASE_CONNECTION_STRING") ?? throw new InvalidOperationException("LOG_DATABASE_CONNECTION_STRING not found in configuration");

        return new DatabaseConfig
        {
            ConnectionString = connectionString
        };
    }

    public LogSystemConfig GetLogSystemConfig()
    {
        var cacheDurationMinutesStr = GetConfigValue("SYSTEM_CACHE_DURATION_MINUTES");
        var defaultLogDurationDaysStr = GetConfigValue("LOGCOLLECTION_DEFAULT_LOG_TTL_DAYS");

        if (string.IsNullOrWhiteSpace(cacheDurationMinutesStr) || !int.TryParse(cacheDurationMinutesStr, out var cacheDurationMinutes))
            throw new InvalidOperationException($"SYSTEM_CACHE_DURATION_MINUTES value '{cacheDurationMinutesStr}' is not a valid integer");

        if (string.IsNullOrWhiteSpace(defaultLogDurationDaysStr) || !int.TryParse(defaultLogDurationDaysStr, out var defaultLogDurationDays))
            throw new InvalidOperationException($"LOGCOLLECTION_DEFAULT_LOG_TTL_DAYS value '{defaultLogDurationDaysStr}' is not a valid int");

        return new LogSystemConfig
        {
            CacheDurationMinutes = TimeSpan.FromMinutes(cacheDurationMinutes),
            DefaultLogDurationDays = defaultLogDurationDays
        };
    }

    public PersistenceBackgroundServiceConfig GetPersistenceBackgroundServiceConfig()
    {
        var rabbitMqConnectionString = GetConfigValue("RABBITMQ_CONNECTION_STRING") ?? throw new InvalidOperationException("RABBITMQ_CONNECTION_STRING not found in configuration");
        var rabbitMqQueueName = GetConfigValue("RABBITMQ_QUEUE_NAME") ?? throw new InvalidOperationException("RABBITMQ_QUEUE_NAME not found in configuration");
        var maxFrequencySecondsStr = GetConfigValue("PERSISTENCE_MAX_FREQUENCY_SECONDS");
        var maxPersistenceBatchSizeStr = GetConfigValue("PERSISTENCE_MAX_BATCH_SIZE");
        var channelCountStr = GetConfigValue("RABBITMQ_CHANNEL_COUNT");
        var prefetchCountStr = GetConfigValue("RABBITMQ_PREFETCH_COUNT");
        
        if (string.IsNullOrWhiteSpace(maxFrequencySecondsStr) || !int.TryParse(maxFrequencySecondsStr, out var maxFrequencySeconds))
            throw new InvalidOperationException($"PERSISTENCE_MAX_FREQUENCY_SECONDS value '{maxFrequencySecondsStr}' is not a valid integer");

        if (string.IsNullOrWhiteSpace(maxPersistenceBatchSizeStr) || !int.TryParse(maxPersistenceBatchSizeStr, out var maxPersistenceBatchSize))
            throw new InvalidOperationException($"PERSISTENCE_MAX_BATCH_SIZE value '{maxPersistenceBatchSizeStr}' is not a valid integer");

        if (string.IsNullOrWhiteSpace(channelCountStr) || !int.TryParse(channelCountStr, out var channelCount))
            throw new InvalidOperationException($"RABBITMQ_CHANNEL_COUNT value '{channelCountStr}' is not a valid integer");

        if (string.IsNullOrWhiteSpace(prefetchCountStr) || !ushort.TryParse(prefetchCountStr, out var prefetchCount))
            throw new InvalidOperationException($"RABBITMQ_PREFETCH_COUNT value '{prefetchCountStr}' is not a valid ushort");

        return new PersistenceBackgroundServiceConfig
        {
            RabbitMqConnectionString = rabbitMqConnectionString,
            RabbitMqQueueName = rabbitMqQueueName,
            MaxFrequency = TimeSpan.FromSeconds(maxFrequencySeconds),
            MaxPersistenceBatchSize = maxPersistenceBatchSize, 
            RabbitMqChannelCount = channelCount,
            RabbitMqPrefetchCount = prefetchCount
        };
    }

    public CleanupBackgroundServiceConfig GetCleanupBackgroundServiceConfig()
    {
        var maxRowsPerBatchStr = GetConfigValue("CLEANUP_MAX_ROWS_PER_BATCH");
        var maxConcurrentCollectionsStr = GetConfigValue("CLEANUP_MAX_CONCURRENT_COLLECTIONS");

        if (string.IsNullOrWhiteSpace(maxRowsPerBatchStr) || !int.TryParse(maxRowsPerBatchStr, out var maxRowsPerBatch))
            throw new InvalidOperationException($"CLEANUP_MAX_ROWS_PER_BATCH value '{maxRowsPerBatchStr}' is not a valid integer");

        if (string.IsNullOrWhiteSpace(maxConcurrentCollectionsStr) || !int.TryParse(maxConcurrentCollectionsStr, out var maxConcurrentCollections))
            throw new InvalidOperationException($"CLEANUP_MAX_CONCURRENT_COLLECTIONS value '{maxConcurrentCollectionsStr}' is not a valid integer");

        return new CleanupBackgroundServiceConfig
        {
            MaxRowsPerBatch = maxRowsPerBatch,
            MaxConcurrentCollections = maxConcurrentCollections
        };
    }

    public PublishServiceConfig GetPublishServiceConfig()
    {
        var rabbitMqConnectionString = GetConfigValue("RABBITMQ_CONNECTION_STRING") ?? throw new InvalidOperationException("RABBITMQ_CONNECTION_STRING not found in configuration");
        var rabbitMqExchangeName = GetConfigValue("RABBITMQ_PERSISTENCE_EXCHANGE_NAME") ?? throw new InvalidOperationException("RABBITMQ_PERSISTENCE_EXCHANGE_NAME not found in configuration");
        var rabbitMqRoutingKey = GetConfigValue("RABBITMQ_PERSISTENCE_ROUTING_KEY") ?? throw new InvalidOperationException("RABBITMQ_PERSISTENCE_ROUTING_KEY not found in configuration");

        return new PublishServiceConfig
        {
            RabbitMqConnectionString = rabbitMqConnectionString,
            RabbitMqExchangeName = rabbitMqExchangeName,
            RabbitMqRoutingKey = rabbitMqRoutingKey
        };
    }

    private string? GetConfigValue(string key)
    {
        if (LocalConfig?.TryGetValue(key, out var localValue) == true)
            return localValue;

        return Environment.GetEnvironmentVariable(key);
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Register configurations
        var azureConfig = GetAzureConfig();
        var databaseConfig = GetDatabaseConfig();
        var logSystemConfig = GetLogSystemConfig();
        var persistenceConfig = GetPersistenceBackgroundServiceConfig();
        var cleanupConfig = GetCleanupBackgroundServiceConfig();
        var publishConfig = GetPublishServiceConfig();

        services.AddSingleton(azureConfig);
        services.AddSingleton(databaseConfig);
        services.AddSingleton(logSystemConfig);
        services.AddSingleton(persistenceConfig);
        services.AddSingleton(cleanupConfig);
        services.AddSingleton(publishConfig);
    }
}