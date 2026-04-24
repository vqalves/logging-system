
using System.Collections.Concurrent;
using LogSystem.Core.Services.Database;

namespace LogSystem.Core.Caching;

public class LogAttributeCache
{
    private readonly TimeSpan CacheDuration;
    private readonly DatabaseService DatabaseService;
    private readonly ConcurrentDictionary<string, CacheEntry<List<LogAttribute>>> Cache = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> LoadLocks = new();

    public LogAttributeCache(TimeSpan cacheDuration, DatabaseService databaseService)
    {
        CacheDuration = cacheDuration;
        DatabaseService = databaseService;
    }

    private string GenerateCacheKey(LogCollection logCollection)
    {
        return $"LogAttributes_{logCollection.ID}";
    }

    private bool IsExpired(CacheEntry<List<LogAttribute>> entry)
    {
        return DateTime.UtcNow - entry.CreatedAt > CacheDuration;
    }

    public async Task<IEnumerable<LogAttribute>> ListAttributesAsync(LogCollection logCollection)
    {
        var cacheKey = GenerateCacheKey(logCollection);

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

            // Load from database and store in cache
            var attributes = await DatabaseService.ListAttributesOfCollectionAsync(logCollection).ToListAsync();

            // Create new cache entry with creation timestamp
            var newEntry = new CacheEntry<List<LogAttribute>>
            {
                Entry = attributes,
                CreatedAt = DateTime.UtcNow
            };

            Cache[cacheKey] = newEntry;

            return attributes;
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
            var cacheKey = GenerateCacheKey(logCollection);
            Cache.TryRemove(cacheKey, out _);
        }
    }
}
