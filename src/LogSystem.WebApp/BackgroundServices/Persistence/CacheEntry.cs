
using System.Collections.Concurrent;
using LogSystem.Core.Services.Database;

namespace LogSystem.WebApp.BackgroundServices.Persistence;

public class CacheEntry<T>
{
    public required T Entry { get; init; }
    public required DateTime CreatedAt { get; init; }
}