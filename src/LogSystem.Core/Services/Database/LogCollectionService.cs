using Microsoft.Data.SqlClient;

namespace LogSystem.Core.Services.Database;

public class LogCollectionService
{
    private readonly DatabaseConfig DatabaseConfig;

    public LogCollectionService(DatabaseConfig databaseConfig)
    {
        DatabaseConfig = databaseConfig;
    }

    public async Task<long> InsertLogCollectionAsync(LogCollection logCollection, SqlConnection connection, SqlTransaction transaction)
    {
        var insertSql = @"
            INSERT INTO [dbo].[LogCollection] ([Name], [ClientId], [TableName], [LogDurationDays], [LifecyclePolicyCreated], [MaxLogsPerFile])
            VALUES (@Name, @ClientId, @TableName, @LogDurationDays, @LifecyclePolicyCreated, @MaxLogsPerFile);
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

        using var insertCommand = new SqlCommand(insertSql, connection, transaction);
        insertCommand.Parameters.AddWithValue("@Name", logCollection.Name);
        insertCommand.Parameters.AddWithValue("@ClientId", logCollection.ClientId);
        insertCommand.Parameters.AddWithValue("@TableName", logCollection.TableName);
        insertCommand.Parameters.AddWithValue("@LogDurationDays", logCollection.LogDurationDays);
        insertCommand.Parameters.AddWithValue("@LifecyclePolicyCreated", logCollection.LifecyclePolicyCreated);
        insertCommand.Parameters.AddWithValue("@MaxLogsPerFile", logCollection.MaxLogsPerFile);

        var newId = await insertCommand.ExecuteScalarAsync();
        return (long)(newId ?? throw new InvalidOperationException("Failed to retrieve new LogCollection ID from database."));
    }

    public async Task UpdateLogCollectionAsync(LogCollection logCollection)
    {
        var updateSql = @"
            UPDATE [dbo].[LogCollection]
            SET [Name] = @Name, [ClientId] = @ClientId, [LogDurationDays] = @LogDurationDays, [LifecyclePolicyCreated] = @LifecyclePolicyCreated, [MaxLogsPerFile] = @MaxLogsPerFile
            WHERE [ID] = @ID;";

        using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
        await connection.OpenAsync();

        using var updateCommand = new SqlCommand(updateSql, connection);
        updateCommand.Parameters.AddWithValue("@Name", logCollection.Name);
        updateCommand.Parameters.AddWithValue("@ClientId", logCollection.ClientId);
        updateCommand.Parameters.AddWithValue("@LogDurationDays", logCollection.LogDurationDays);
        updateCommand.Parameters.AddWithValue("@LifecyclePolicyCreated", logCollection.LifecyclePolicyCreated);
        updateCommand.Parameters.AddWithValue("@MaxLogsPerFile", logCollection.MaxLogsPerFile);
        updateCommand.Parameters.AddWithValue("@ID", logCollection.ID);

        await updateCommand.ExecuteNonQueryAsync();
    }

    public async Task DeleteLogCollectionRecordAsync(long logCollectionId, SqlConnection connection, SqlTransaction transaction)
    {
        var deleteCollectionSql = @"
            DELETE FROM [dbo].[LogCollection]
            WHERE [ID] = @ID;";

        using var deleteCollectionCommand = new SqlCommand(deleteCollectionSql, connection, transaction);
        deleteCollectionCommand.Parameters.AddWithValue("@ID", logCollectionId);
        await deleteCollectionCommand.ExecuteNonQueryAsync();
    }

    public async Task DeleteAttributesOfCollectionAsync(long logCollectionId, SqlConnection connection, SqlTransaction transaction)
    {
        var deleteAttributesSql = @"
            DELETE FROM [dbo].[LogCollectionAttribute]
            WHERE [LogCollectionID] = @LogCollectionID;";

        using var deleteAttributesCommand = new SqlCommand(deleteAttributesSql, connection, transaction);
        deleteAttributesCommand.Parameters.AddWithValue("@LogCollectionID", logCollectionId);
        await deleteAttributesCommand.ExecuteNonQueryAsync();
    }

    public async Task CreateLogTableAsync(LogCollection logCollection, SqlConnection connection, SqlTransaction transaction)
    {
        var createTableSql = $@"
            CREATE TABLE [logcollection].[{logCollection.TableName}]
            (
                [ID] BIGINT IDENTITY(1,1) NOT NULL,
                [SourceFileIndex] INT NOT NULL,
                [SourceFileName] VARCHAR(100) NOT NULL,
                [ValidUntilUtc] DATETIME2 NOT NULL,
                CONSTRAINT [PK_{logCollection.TableName}] PRIMARY KEY CLUSTERED ([ID] ASC)
            );";

        using var createTableCommand = new SqlCommand(createTableSql, connection, transaction);
        await createTableCommand.ExecuteNonQueryAsync();
    }

    public async Task CreateValidUntilIndexAsync(LogCollection logCollection, SqlConnection connection, SqlTransaction transaction)
    {
        var createIndexSql = $@"
            CREATE NONCLUSTERED INDEX [IX_{logCollection.TableName}_ValidUntilUtc]
            ON [logcollection].[{logCollection.TableName}] ([ValidUntilUtc] ASC);";

        using var createIndexCommand = new SqlCommand(createIndexSql, connection, transaction);
        await createIndexCommand.ExecuteNonQueryAsync();
    }

    public async Task DropLogTableAsync(LogCollection logCollection, SqlConnection connection, SqlTransaction transaction)
    {
        var dropTableSql = $@"
            IF OBJECT_ID('[logcollection].[{logCollection.TableName}]', 'U') IS NOT NULL
                DROP TABLE [logcollection].[{logCollection.TableName}];";

        using var dropTableCommand = new SqlCommand(dropTableSql, connection, transaction);
        await dropTableCommand.ExecuteNonQueryAsync();
    }

    public async Task<LogCollection?> GetLogCollectionByIdAsync(long? id)
    {
        await foreach (var collection in ListLogCollectionsAsync(id: id))
            return collection;

        return null;
    }

    public async Task<LogCollection?> GetLogCollectionByClientIdAsync(string clientId)
    {
        await foreach (var collection in ListLogCollectionsAsync(clientIdEquals: clientId))
            return collection;

        return null;
    }

    public async IAsyncEnumerable<LogCollection> ListLogCollectionsAsync(long? id = null, string? clientIdEquals = null, string? tableNameEquals = null)
    {
        var whereClauses = new List<string>();
        var parameters = new List<SqlParameter>();

        if (id.HasValue)
        {
            whereClauses.Add("[ID] = @ID");
            parameters.Add(new SqlParameter("@ID", id.Value));
        }

        if (!string.IsNullOrWhiteSpace(clientIdEquals))
        {
            whereClauses.Add("[ClientId] = @ClientId");
            parameters.Add(new SqlParameter("@ClientId", clientIdEquals));
        }

        if (!string.IsNullOrWhiteSpace(tableNameEquals))
        {
            whereClauses.Add("[TableName] = @TableName");
            parameters.Add(new SqlParameter("@TableName", tableNameEquals));
        }

        var sql = @"
            SELECT [ID], [Name], [ClientId], [TableName], [LogDurationDays], [LifecyclePolicyCreated], [MaxLogsPerFile]
            FROM [dbo].[LogCollection]";

        if (whereClauses.Count > 0)
            sql += " WHERE " + string.Join(" AND ", whereClauses);

        using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters.ToArray());

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var logCollection = new LogCollection(
                name: reader.GetString(1),
                clientId: reader.GetString(2),
                tableName: reader.GetString(3),
                logDurationDays: reader.GetInt32(4),
                maxLogsPerFile: reader.GetInt32(6));

            logCollection.ID = reader.GetInt64(0);
            logCollection.LifecyclePolicyCreated = reader.GetBoolean(5);
            yield return logCollection;
        }
    }
}
