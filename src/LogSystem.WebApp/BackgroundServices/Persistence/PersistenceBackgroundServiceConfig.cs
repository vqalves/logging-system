
namespace LogSystem.WebApp.BackgroundServices.Persistence;

public class PersistenceBackgroundServiceConfig
{
    public required string RabbitMqConnectionString { get; init; }
    public required string RabbitMqQueueName { get; init; }
    public required TimeSpan MaxFrequency { get; init; }
}