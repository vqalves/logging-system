using RabbitMQ.Client;

namespace LogSystem.WebApp.Services;

public class RabbitMqPublisher : IAsyncDisposable, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly PublishServiceConfig _config;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private bool _disposed;

    private RabbitMqPublisher(
        IConnection connection,
        IChannel channel,
        PublishServiceConfig config,
        ILogger<RabbitMqPublisher> logger)
    {
        _connection = connection;
        _channel = channel;
        _config = config;
        _logger = logger;
    }

    public static async Task<RabbitMqPublisher> CreateAsync(
        PublishServiceConfig config,
        ILogger<RabbitMqPublisher> logger)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(config.RabbitMqConnectionString)
            };

            var connection = await factory.CreateConnectionAsync();
            var channel = await connection.CreateChannelAsync();

            logger.LogInformation("RabbitMqPublisher initialized successfully");

            return new RabbitMqPublisher(connection, channel, config, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize RabbitMqPublisher");
            throw;
        }
    }

    public async Task PublishAsync(string exchange, string routingKey, ReadOnlyMemory<byte> message)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            await _channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: routingKey,
                body: message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to exchange {Exchange} with routing key {RoutingKey}", exchange, routingKey);
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _channel?.Dispose();
        _connection?.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);

        _logger.LogInformation("RabbitMqPublisher disposed");
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        if (_channel != null)
            await _channel.DisposeAsync();
        if (_connection != null)
            await _connection.DisposeAsync();

        _disposed = true;
        GC.SuppressFinalize(this);

        _logger.LogInformation("RabbitMqPublisher disposed");
    }
}
