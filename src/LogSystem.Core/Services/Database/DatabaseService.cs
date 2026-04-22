using Microsoft.Data.SqlClient;

namespace LogSystem.Core.Services.Database;

public class DatabaseService
{
    private readonly DatabaseConfig DatabaseConfig;
    private readonly LogAttributeService LogAttributeService;
    private readonly LogCollectionService LogCollectionService;
    private readonly LogDataService LogDataService;

    public DatabaseService(
        DatabaseConfig databaseConfig,
        LogAttributeService logAttributeService,
        LogCollectionService logCollectionService,
        LogDataService logDataService)
    {
        DatabaseConfig = databaseConfig;
        LogAttributeService = logAttributeService;
        LogCollectionService = logCollectionService;
        LogDataService = logDataService;
    }

    public async Task CreateAttributeAsync(LogCollection logCollection, LogAttribute logAttribute)
    {
        using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
        await connection.OpenAsync();

        // Wrap insert + ALTER TABLE in transaction for atomicity
        using var transaction = connection.BeginTransaction();
        try
        {
            // Insert into LogCollectionAttribute table and retrieve the new ID
            var newId = await LogAttributeService.InsertAttributeAsync(logAttribute, connection, transaction);
            logAttribute.ID = newId;

            // Execute ALTER TABLE to add the column
            await LogAttributeService.AlterTableAddColumnAsync(logCollection, logAttribute, connection, transaction);

            // Create filtered index on the column
            await LogAttributeService.CreateIndexOnColumnAsync(logCollection, logAttribute, connection, transaction);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task DeleteAttributeAsync(LogCollection logCollection, LogAttribute logAttribute)
    {
        using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
        await connection.OpenAsync();

        // Wrap delete + DROP operations in transaction for atomicity
        using var transaction = connection.BeginTransaction();
        try
        {
            // Delete record from LogCollectionAttribute table
            await LogAttributeService.DeleteAttributeRecordAsync(logAttribute.ID, connection, transaction);

            bool columnExists = await LogAttributeService.DoesColumnExistAsync(logCollection, logAttribute, connection, transaction);

            if (columnExists)
            {
                List<string> indexNames = await LogAttributeService.ListIndexesOfAttributeAsync(logCollection, logAttribute, connection, transaction);

                foreach (var indexName in indexNames)
                    await LogAttributeService.DropIndexAsync(logCollection, indexName, connection, transaction);

                await LogAttributeService.DropColumnAsync(logCollection, logAttribute, connection, transaction);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task UpdateLogAttributeAsync(LogAttribute logAttribute, string newExtractionExpression)
    {
        await LogAttributeService.UpdateAttributeAsync(logAttribute.ID, newExtractionExpression);
    }

    public async Task<LogCollection?> GetLogCollectionByClientIdAsync(string clientId)
    {
        return await LogCollectionService.GetLogCollectionByClientIdAsync(clientId);
    }

    public async Task<LogCollection?> GetLogCollectionByIdAsync(long? id)
    {
        return await LogCollectionService.GetLogCollectionByIdAsync(id);
    }

    public async Task SaveLogCollectionAsync(LogCollection logCollection)
    {
        using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
        await connection.OpenAsync();

        if (logCollection.ID == 0)
        {
            // INSERT: Create new log collection and dynamic log table
            using var transaction = connection.BeginTransaction();
            try
            {
                // Insert into LogCollection table and retrieve the new ID
                var newId = await LogCollectionService.InsertLogCollectionAsync(logCollection, connection, transaction);
                logCollection.ID = newId;

                // Create dynamic log table with fixed columns
                await LogCollectionService.CreateLogTableAsync(logCollection, connection, transaction);

                // Add index on ValidUntilUtc for cleanup queries
                await LogCollectionService.CreateValidUntilIndexAsync(logCollection, connection, transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        else
        {
            // UPDATE: Update existing log collection (TableName is immutable)
            await LogCollectionService.UpdateLogCollectionAsync(logCollection);
        }
    }

    public async Task SaveLogsAsync(LogCollection logCollection, IEnumerable<Log> logs)
    {
        await LogDataService.SaveLogsAsync(logCollection, logs);
    }

    public async Task<int> DeleteExpiredLogsAsync(LogCollection logCollection, int maxRows)
    {
        return await LogDataService.DeleteExpiredLogsAsync(logCollection, maxRows);
    }

    public async IAsyncEnumerable<LogAttribute> ListLogAttributesAsync()
    {
        await foreach (var attribute in LogAttributeService.ListAttributesAsync())
            yield return attribute;
    }

    public async IAsyncEnumerable<LogCollection> ListLogCollectionsAsync(long? id = null, string? clientIdEquals = null, string? tableNameEquals = null)
    {
        await foreach (var collection in LogCollectionService.ListLogCollectionsAsync(id, clientIdEquals, tableNameEquals))
            yield return collection;
    }

    public async Task DeleteLogCollectionAsync(LogCollection logCollection)
    {
        using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        try
        {
            // Delete all associated attributes
            await LogCollectionService.DeleteAttributesOfCollectionAsync(logCollection.ID, connection, transaction);

            // Drop the dynamic table
            await LogCollectionService.DropLogTableAsync(logCollection, connection, transaction);

            // Delete the LogCollection record
            await LogCollectionService.DeleteLogCollectionRecordAsync(logCollection.ID, connection, transaction);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async IAsyncEnumerable<LogAttribute> ListAttributesOfCollectionAsync(LogCollection logCollection)
    {
        await foreach (var attribute in LogAttributeService.ListAttributesOfCollectionAsync(logCollection))
            yield return attribute;
    }

    public async IAsyncEnumerable<Log> QueryLogsAsync(LogCollection logCollection, IEnumerable<LogAttribute> attributes, IEnumerable<LogFilter> filters, long? lastId = null, int limit = 100)
    {
        var enumerator = LogDataService.QueryLogsAsync(
            logCollection: logCollection, 
            attributes: attributes, 
            filters: filters, 
            lastId: lastId, 
            limit: limit);

        await foreach (var log in enumerator)
            yield return log;
    }
}