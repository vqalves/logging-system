using System.Text.Json;
using LogSystem.Core.Services.Database;
using RabbitMQ.Client;

namespace LogSystem.WebApp.BackgroundServices.Persistence;

public class ReceivedMessageModel : IDisposable
{
    public required IChannel Channel { get; init; }
    public required ulong DeliveryTag { get; init; }
    public required string Payload { get; init; }
    public required PersistenceStatus Status { get; set; }
    public Log? Log { get; set; }

    private Lazy<JsonDocument?>? _jsonDocument;
    private bool _disposed = false;

    public JsonDocument? GetJsonDocument()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ReceivedMessageModel));

        _jsonDocument ??= new Lazy<JsonDocument?>(() =>
        {
            try { return JsonDocument.Parse(Payload); }
            catch(JsonException) { return null; }
        });

        return _jsonDocument.Value;
    }

    public string GetLogCollectionClientId()
    {
        var document = GetJsonDocument();

        if(document != null)
            if (document.RootElement.TryGetProperty("Properties", out var properties))
                if(properties.TryGetProperty("LogCollectionClientId", out var logCollectionClientId))
                    return logCollectionClientId.GetString() ?? string.Empty;

        throw new ArgumentException("Cannot retrieve collection name from message");
    }

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

    public enum PersistenceStatus
    {
        Pending, Persisted, Failed
    }
}