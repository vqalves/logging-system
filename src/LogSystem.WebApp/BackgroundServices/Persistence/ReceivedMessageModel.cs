using System.Text.Json;
using RabbitMQ.Client;

namespace LogSystem.WebApp.BackgroundServices.Persistence;

public class ReceivedMessageModel
{
    public required IModel Channel { get; init; }
    public required ulong DeliveryTag { get; init; }
    public required string Payload { get; init; }
    public required PersistenceStatus Status { get; set; }

    private JsonDocument? _jsonDocument;
    private JsonDocument GetJsonDocument()
    {
        _jsonDocument ??= JsonDocument.Parse(Payload);
        return _jsonDocument;
    }

    public string GetLogCollectionName()
    {
        var document = GetJsonDocument();

        if (document.RootElement.TryGetProperty("Properties", out var properties) &&
            properties.TryGetProperty("LogCollectionName", out var logCollectionName))
        {
            return logCollectionName.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    public enum PersistenceStatus
    {
        Pending, Persisted, Failed
    }
}