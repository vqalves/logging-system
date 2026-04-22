using System.Text.Json;
using LogSystem.Core.Services.Database;
using RabbitMQ.Client;

namespace LogSystem.WebApp.BackgroundServices.Persistence;

public class ReceivedMessageModel
{
    public required IChannel Channel { get; init; }
    public required ulong DeliveryTag { get; init; }
    public required string Payload { get; init; }
    public required PersistenceStatus Status { get; set; }
    public Log? Log { get; set; }

    private Lazy<JsonDocument?>? _jsonDocument;
    public JsonDocument? GetJsonDocument()
    {
        _jsonDocument ??= new Lazy<JsonDocument?>(() =>
        {
            try { return JsonDocument.Parse(Payload); }
            catch(JsonException) { return null; }
        });

        return _jsonDocument.Value;
    }

    public string GetLogCollectionName()
    {
        var document = GetJsonDocument();

        if(document != null)
            if (document.RootElement.TryGetProperty("Properties", out var properties))
                if(properties.TryGetProperty("LogCollectionName", out var logCollectionName))
                    return logCollectionName.GetString() ?? string.Empty;

        throw new ArgumentException("Cannot retrieve collection name from message");
    }

    public enum PersistenceStatus
    {
        Pending, Persisted, Failed
    }
}