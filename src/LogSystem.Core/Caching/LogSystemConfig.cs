
namespace LogSystem.Core.Caching;

public class LogSystemConfig
{
    public required TimeSpan CacheDurationMinutes { get; init; }
    public required int DefaultLogDurationDays { get; init; }
}
