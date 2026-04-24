using System.Text.Json;
using LogSystem.Core.Services.Database;
using RabbitMQ.Client;

namespace LogSystem.Core.BackgroundServices.Persistence.DefaultMessageReceiver;

public class DefaultReceivedMessageModel : IReceivedMessageModel, IDisposable
{
    public required IChannel Channel { get; init; }
    public required ulong DeliveryTag { get; init; }
    public required string Payload { get; init; }
    public required IReceivedMessageModel.PersistenceStatus Status { get; set; }
    public Log? Log { get; set; }

    private Lazy<JsonDocument?>? _jsonDocument;
    private bool _disposed = false;

    public JsonDocument? GetPayloadAsJsonDocument()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DefaultReceivedMessageModel));

        _jsonDocument ??= new Lazy<JsonDocument?>(() =>
        {
            try { return JsonDocument.Parse(Payload); }
            catch(JsonException) { return null; }
        });

        return _jsonDocument.Value;
    }

    public string GetLogCollectionClientId()
    {
        var document = GetPayloadAsJsonDocument();

        if(document != null)
            if (document.RootElement.TryGetProperty("Properties", out var properties))
                if(properties.TryGetProperty("LogCollectionClientId", out var logCollectionClientId))
                    return logCollectionClientId.GetString() ?? string.Empty;

        throw new ArgumentException("Cannot retrieve collection name from message");
    }

    public string GetPayloadAsString() => Payload;
    public Log GetLog() => Log!;
    public IChannel GetRabbitChannel() => Channel;
    public ulong GetRabbitMqDeliveryTag() => DeliveryTag;

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_jsonDocument?.IsValueCreated == true)
            {
                _jsonDocument.Value?.Dispose();
            }
            _disposed = true;
        }
    }
}