
using LogSystem.Core.Services.Azure;
using LogSystem.Core.Services.Database;
using LogSystem.WebApp.BackgroundServices.Persistence;
using System.Text.Json;

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
            LocalConfig = JsonSerializer.Deserialize<Dictionary<string, string>>(localJson);
        }
    }

    public AzureConfig GetAzureConfig()
    {
        var connectionString = GetConfigValue("AZURE_BLOB_STORAGE_CONNECTION_STRING") ?? throw new InvalidOperationException("AZURE_BLOB_STORAGE_CONNECTION_STRING not found in configuration");
        var containerName = GetConfigValue("AZURE_BLOB_STORAGE_CONTAINER_NAME") ?? "logs";

        return new AzureConfig
        {
            ConnectionString = connectionString,
            ContainerName = containerName
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
        var defaultLogDurationHoursStr = GetConfigValue("LOGCOLLECTION_DEFAULT_LOG_TTL_HOURS");

        if (string.IsNullOrWhiteSpace(cacheDurationMinutesStr) || !int.TryParse(cacheDurationMinutesStr, out var cacheDurationMinutes))
            throw new InvalidOperationException($"SYSTEM_CACHE_DURATION_MINUTES value '{cacheDurationMinutesStr}' is not a valid integer");

        if (string.IsNullOrWhiteSpace(defaultLogDurationHoursStr) || !long.TryParse(defaultLogDurationHoursStr, out var defaultLogDurationHours))
            throw new InvalidOperationException($"LOGCOLLECTION_DEFAULT_LOG_TTL_HOURS value '{defaultLogDurationHoursStr}' is not a valid long");

        return new LogSystemConfig
        {
            CacheDurationMinutes = TimeSpan.FromMinutes(cacheDurationMinutes),
            DefaultLogDurationHours = defaultLogDurationHours
        };
    }

    public PersistenceBackgroundServiceConfig GetPersistenceBackgroundServiceConfig()
    {
        var rabbitMqConnectionString = GetConfigValue("RABBITMQ_CONNECTION_STRING") ?? throw new InvalidOperationException("RABBITMQ_CONNECTION_STRING not found in configuration");
        var rabbitMqQueueName = GetConfigValue("RABBITMQ_QUEUE_NAME") ?? throw new InvalidOperationException("RABBITMQ_QUEUE_NAME not found in configuration");
        var maxFrequencySecondsStr = GetConfigValue("PERSISTENCE_MAX_FREQUENCY_SECONDS");

        if (string.IsNullOrWhiteSpace(maxFrequencySecondsStr) || !int.TryParse(maxFrequencySecondsStr, out var maxFrequencySeconds))
            throw new InvalidOperationException($"PERSISTENCE_MAX_FREQUENCY_SECONDS value '{maxFrequencySecondsStr}' is not a valid integer");

        return new PersistenceBackgroundServiceConfig
        {
            RabbitMqConnectionString = rabbitMqConnectionString,
            RabbitMqQueueName = rabbitMqQueueName,
            MaxFrequency = TimeSpan.FromSeconds(maxFrequencySeconds)
        };
    }

    private string? GetConfigValue(string key)
    {
        if (LocalConfig?.TryGetValue(key, out var localValue) == true)
            return localValue;

        return Environment.GetEnvironmentVariable(key);
    }
}