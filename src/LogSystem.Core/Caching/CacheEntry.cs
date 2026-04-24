
namespace LogSystem.Core.Caching;

public class CacheEntry<T>
{
    public required T Entry { get; init; }
    public required DateTime CreatedAt { get; init; }
}
