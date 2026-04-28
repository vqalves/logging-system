
using System.Collections.Concurrent;
using LogSystem.Core.Services.Azure;
using LogSystem.Core.Services.Database;

namespace LogSystem.Core.Caching;

public class LogCollectionCache
{
    private readonly TimeSpan CacheDuration;
    private readonly DatabaseService DatabaseService;
    private readonly AzureService AzureService;
    private readonly ConcurrentDictionary<string, CacheEntry<LogCollection>> Cache = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> LoadLocks = new();
    private readonly LogSystemConfig LogSystemConfig;

    public LogCollectionCache(TimeSpan cacheDuration, DatabaseService databaseService, LogSystemConfig logSystemConfig, AzureService azureService)
    {
        CacheDuration = cacheDuration;
        DatabaseService = databaseService;
        LogSystemConfig = logSystemConfig;
        AzureService = azureService;
    }

    private string GenerateCacheKey(string clientId)
    {
        return $"LogCollection_{clientId}";
    }

    private bool IsExpired(CacheEntry<LogCollection> entry)
    {
        return DateTime.UtcNow - entry.CreatedAt > CacheDuration;
    }

    public async Task<LogCollection> GetOrCreateByClientIdAsync(string clientId)
    {
        var cacheKey = GenerateCacheKey(clientId);

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
            var logCollection = await DatabaseService.GetLogCollectionByClientIdAsync(clientId);

            // If not found in database, create new collection
            if (logCollection == null)
                logCollection = await CreateLogCollectionAsync(clientId);

            // Check if lifecycle policy needs to be created (inside the lock for thread-safety)
            if (!logCollection.LifecyclePolicyCreated)
            {
                await AzureService.SaveLifecyclePolicyAsync(logCollection);
                
                logCollection.LifecyclePolicyCreated = true;
                await DatabaseService.SaveLogCollectionAsync(logCollection);
            }

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

    private async Task<LogCollection> CreateLogCollectionAsync(string collectionName)
    {
        // Create new LogCollection
        var newLogCollection = new LogCollection(
            name: collectionName,
            clientId: collectionName,
            tableName: $"{collectionName}",
            logDurationDays: LogSystemConfig.DefaultLogDurationDays);

        // Save to database
        await DatabaseService.SaveLogCollectionAsync(newLogCollection);

        // Timestamp
        var timestampAttribute = new LogAttribute(
            logCollectionID: newLogCollection.ID,
            name: "Timestamp",
            sqlColumnName: "Timestamp",
            attributeTypeID: AttributeType.DateTime.Value,
            extractionStyleID: ExtractionStyle.JSON.Value,
            extractionExpression: "$.Timestamp");

        await DatabaseService.CreateAttributeAsync(newLogCollection, timestampAttribute);

        var logLevelAttribute = new LogAttribute(
            logCollectionID: newLogCollection.ID,
            name: "Log Level",
            sqlColumnName: "LogLevel",
            attributeTypeID: AttributeType.Text.Value,
            extractionStyleID: ExtractionStyle.JSON.Value,
            extractionExpression: "$.Level");

        await DatabaseService.CreateAttributeAsync(newLogCollection, logLevelAttribute);

        var exceptionAttribute = new LogAttribute(
            logCollectionID: newLogCollection.ID,
            name: "Exception",
            sqlColumnName: "Exception",
            attributeTypeID: AttributeType.Text.Value,
            extractionStyleID: ExtractionStyle.JSON.Value,
            extractionExpression: "$.Exception.Message");

        await DatabaseService.CreateAttributeAsync(newLogCollection, exceptionAttribute);

        return newLogCollection;
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
            var cacheKey = GenerateCacheKey(logCollection.ClientId);
            Cache.TryRemove(cacheKey, out _);
        }
    }
}
