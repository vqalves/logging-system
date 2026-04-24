
namespace LogSystem.Core.Services.RabbitMq;

public class PublishServiceConfig
{
    public required string RabbitMqConnectionString { get; init; }
    public required string RabbitMqExchangeName { get; init; }
    public required string RabbitMqRoutingKey { get; init; }
}
