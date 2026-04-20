
namespace LogSystem.WebApp.BackgroundServices.Persistence;

public class LogSystemConfig
{
    public required TimeSpan CacheDurationMinutes { get; init; }
    public required long DefaultLogDurationHours { get; init; }
}
