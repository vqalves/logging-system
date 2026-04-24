using RabbitMQ.Client;
using Microsoft.Extensions.Logging;

namespace LogSystem.Core.Services.RabbitMq;

public class RabbitMqPublisher : IAsyncDisposable, IDisposable
{
    private readonly PublishServiceConfig _config;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;
    private bool _disposed;
    private bool _initialized;

    public RabbitMqPublisher(
        PublishServiceConfig config,
        ILogger<RabbitMqPublisher> logger)
    {
        _config = config;
        _logger = logger;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized)
            return;

        await _initializationLock.WaitAsync();
        try
        {
            if (_initialized)
                return;

            var factory = new ConnectionFactory
            {
                Uri = new Uri(_config.RabbitMqConnectionString)
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            _initialized = true;
            _logger.LogInformation("RabbitMqPublisher initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMqPublisher");
            throw;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    public async Task PublishAsync(string exchange, string routingKey, ReadOnlyMemory<byte> message)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await EnsureInitializedAsync();

        try
        {
            await _channel!.BasicPublishAsync(
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
        _initializationLock.Dispose();
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

        _initializationLock.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);

        _logger.LogInformation("RabbitMqPublisher disposed");
    }
}
