
using System.Collections.Concurrent;
using LogSystem.Core.Services.Database;

namespace LogSystem.WebApp.BackgroundServices.Persistence;

public class LogCollectionCache
{
    private readonly TimeSpan CacheDuration;
    private readonly DatabaseService DatabaseService;
    private readonly ConcurrentDictionary<string, CacheEntry<LogCollection>> Cache = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> LoadLocks = new();

    public LogCollectionCache(TimeSpan cacheDuration, DatabaseService databaseService)
    {
        CacheDuration = cacheDuration;
        DatabaseService = databaseService;
    }

    private string GenerateCacheKey(string collectionName)
    {
        return $"LogCollection_{collectionName}";
    }

    private bool IsExpired(CacheEntry<LogCollection> entry)
    {
        return DateTime.UtcNow - entry.CreatedAt > CacheDuration;
    }

    public async Task<LogCollection> GetByNameAsync(string collectionName, Func<Task<LogCollection>> onNotFound)
    {
        var cacheKey = GenerateCacheKey(collectionName);

        // Try to get from cache and check if not expired (lazy expiration)
        if (Cache.TryGetValue(cacheKey, out var cacheEntry) && !IsExpired(cacheEntry))
            return cacheEntry.Entry;

        // Cache miss or expired - need to load from database
        // Get or create a semaphore specific to this cache key
        var loadLock = LoadLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));

        await loadLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock (another thread may have loaded it)
            if (Cache.TryGetValue(cacheKey, out cacheEntry) && !IsExpired(cacheEntry))
                return cacheEntry.Entry;

            // Load from database
            var logCollection = await DatabaseService.GetLogCollectionByNameAsync(collectionName);

            // If not found in database, execute the onNotFound callback
            if (logCollection == null)
                logCollection = await onNotFound();

            // Create new cache entry with creation timestamp
            var newEntry = new CacheEntry<LogCollection>
            {
                Entry = logCollection,
                CreatedAt = DateTime.UtcNow
            };

            Cache[cacheKey] = newEntry;

            return logCollection;
        }
        finally
        {
            loadLock.Release();
        }
    }

    public void InvalidateCache(LogCollection? logCollection)
    {
        if (logCollection == null)
        {
            // Invalidate all cached collections
            Cache.Clear();
        }
        else
        {
            // Invalidate specific log collection
            var cacheKey = GenerateCacheKey(logCollection.Name);
            Cache.TryRemove(cacheKey, out _);
        }
    }
}
