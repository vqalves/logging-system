
namespace LogSystem.Core.BackgroundServices.Persistence;

public class PersistenceBackgroundServiceConfig
{
    public required string RabbitMqConnectionString { get; init; }
    public required string RabbitMqQueueName { get; init; }
    public required TimeSpan MaxFrequency { get; init; }
    public required int MaxPersistenceBatchSize { get; init; }
    public required int RabbitMqChannelCount { get; init; }
    public required ushort RabbitMqPrefetchCount { get; init; }
}