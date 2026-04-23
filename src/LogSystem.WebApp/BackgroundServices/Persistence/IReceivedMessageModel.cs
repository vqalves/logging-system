using System.Text.Json;
using LogSystem.Core.Services.Database;
using RabbitMQ.Client;

namespace LogSystem.WebApp.BackgroundServices.Persistence.DefaultMessageReceiver;

public interface IReceivedMessageModel : IDisposable
{
    JsonDocument? GetPayloadAsJsonDocument();

    string GetPayloadAsString();

    string GetLogCollectionClientId();

    Log GetLog();

    IChannel GetRabbitChannel();

    ulong GetRabbitMqDeliveryTag();

    PersistenceStatus Status { get; set; }

    public enum PersistenceStatus
    {
        Pending, Persisted, Failed
    }
}