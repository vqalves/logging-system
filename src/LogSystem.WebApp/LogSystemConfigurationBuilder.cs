
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
        var connectionString = GetConfigValue("AzureConnectionString") ?? throw new InvalidOperationException("AzureConnectionString not found in configuration");
        var containerName = GetConfigValue("AzureContainerName") ?? "logs";

        return new AzureConfig
        {
            ConnectionString = connectionString,
            ContainerName = containerName
        };
    }

    public DatabaseConfig GetDatabaseConfig()
    {
        var connectionString = GetConfigValue("DatabaseConnectionString") ?? throw new InvalidOperationException("DatabaseConnectionString not found in configuration");

        return new DatabaseConfig
        {
            ConnectionString = connectionString
        };
    }

    public PersistenceBackgroundServiceConfig GetPersistenceBackgroundServiceConfig()
    {
        var rabbitMqConnectionString = GetConfigValue("RabbitMqConnectionString") ?? throw new InvalidOperationException("RabbitMqConnectionString not found in configuration");
        var rabbitMqQueueName = GetConfigValue("RabbitMqQueueName") ?? throw new InvalidOperationException("RabbitMqQueueName not found in configuration");

        return new PersistenceBackgroundServiceConfig
        {
            RabbitMqConnectionString = rabbitMqConnectionString,
            RabbitMqQueueName = rabbitMqQueueName
        };
    }

    private string? GetConfigValue(string key)
    {
        if (LocalConfig?.TryGetValue(key, out var localValue) == true)
            return localValue;

        return Environment.GetEnvironmentVariable(key);
    }
}