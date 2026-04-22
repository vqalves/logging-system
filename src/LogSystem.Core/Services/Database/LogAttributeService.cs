using System.Data;
using Microsoft.Data.SqlClient;

namespace LogSystem.Core.Services.Database;

public class LogAttributeService
{
    private readonly DatabaseConfig DatabaseConfig;

    public LogAttributeService(DatabaseConfig databaseConfig)
    {
        DatabaseConfig = databaseConfig;
    }

    public async Task<long> InsertAttributeAsync(LogAttribute logAttribute, SqlConnection connection, SqlTransaction transaction)
    {
        var insertSql = @"
            INSERT INTO [dbo].[LogCollectionAttribute] ([LogCollectionID], [Name], [SqlColumnName], [AttributeTypeID], [ExtractionStyleID], [ExtractionExpression])
            VALUES (@LogCollectionID, @Name, @SqlColumnName, @AttributeTypeID, @ExtractionStyleID, @ExtractionExpression);
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

        using var insertCommand = new SqlCommand(insertSql, connection, transaction);
        insertCommand.Parameters.AddWithValue("@LogCollectionID", logAttribute.LogCollectionID);
        insertCommand.Parameters.AddWithValue("@Name", logAttribute.Name);
        insertCommand.Parameters.AddWithValue("@SqlColumnName", logAttribute.SqlColumnName);
        insertCommand.Parameters.AddWithValue("@AttributeTypeID", logAttribute.AttributeTypeID);
        insertCommand.Parameters.AddWithValue("@ExtractionStyleID", logAttribute.ExtractionStyleID);
        insertCommand.Parameters.AddWithValue("@ExtractionExpression", logAttribute.ExtractionExpression);

        var newId = await insertCommand.ExecuteScalarAsync();
        return (long)(newId ?? throw new InvalidOperationException("Failed to retrieve new LogAttribute ID from database."));
    }

    public async Task UpdateAttributeAsync(long attributeId, string newExtractionExpression)
    {
        var updateSql = @"
            UPDATE [dbo].[LogCollectionAttribute]
            SET [ExtractionExpression] = @ExtractionExpression
            WHERE [ID] = @ID;";

        using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(updateSql, connection);
        command.Parameters.AddWithValue("@ID", attributeId);
        command.Parameters.AddWithValue("@ExtractionExpression", newExtractionExpression);

        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteAttributeRecordAsync(long attributeId, SqlConnection connection, SqlTransaction transaction)
    {
        var deleteSql = @"
            DELETE FROM [dbo].[LogCollectionAttribute]
            WHERE [ID] = @ID;";

        using var deleteCommand = new SqlCommand(deleteSql, connection, transaction);
        deleteCommand.Parameters.AddWithValue("@ID", attributeId);
        await deleteCommand.ExecuteNonQueryAsync();
    }

    public async Task AlterTableAddColumnAsync(LogCollection logCollection, LogAttribute logAttribute, SqlConnection connection, SqlTransaction transaction)
    {
        // Get the SQL data type from the AttributeType
        var attributeType = AttributeType.Parse(logAttribute.AttributeTypeID);
        var sqlDataType = attributeType!.SqlDataType;

        // For VARCHAR columns, add collation
        if (attributeType == AttributeType.Text)
            sqlDataType += " COLLATE SQL_Latin1_General_CP1_CI_AS";

        var alterTableSql = $@"
            ALTER TABLE [logcollection].[{logCollection.TableName}]
            ADD [{logAttribute.SqlColumnName}] {sqlDataType} NULL;";

        using var alterTableCommand = new SqlCommand(alterTableSql, connection, transaction);
        await alterTableCommand.ExecuteNonQueryAsync();
    }

    public async Task CreateIndexOnColumnAsync(LogCollection logCollection, LogAttribute logAttribute, SqlConnection connection, SqlTransaction transaction)
    {
        var createIndexSql = $@"
            CREATE INDEX [IX_{logCollection.TableName}_{logAttribute.SqlColumnName}]
            ON [logcollection].[{logCollection.TableName}] ([{logAttribute.SqlColumnName}])
            WHERE [{logAttribute.SqlColumnName}] IS NOT NULL;";

        using var createIndexCommand = new SqlCommand(createIndexSql, connection, transaction);
        await createIndexCommand.ExecuteNonQueryAsync();
    }

    public async Task DropColumnAsync(LogCollection logCollection, LogAttribute logAttribute, SqlConnection connection, SqlTransaction transaction)
    {
        var alterTableSql = $@"ALTER TABLE [logcollection].[{logCollection.TableName}] DROP COLUMN [{logAttribute.SqlColumnName}];";

        using var alterTableCommand = new SqlCommand(alterTableSql, connection, transaction);
        await alterTableCommand.ExecuteNonQueryAsync();
    }

    public async Task DropIndexAsync(LogCollection logCollection, string indexName, SqlConnection connection, SqlTransaction transaction)
    {
        var dropIndexSql = $@"DROP INDEX [{indexName}] ON [logcollection].[{logCollection.TableName}];";

        using var dropIndexCommand = new SqlCommand(dropIndexSql, connection, transaction);
        await dropIndexCommand.ExecuteNonQueryAsync();
    }

    internal async Task<bool> DoesColumnExistAsync(LogCollection logCollection, LogAttribute logAttribute, SqlConnection connection, SqlTransaction transaction)
    {
        var checkColumnSql = @"
            SELECT COUNT(*)
            FROM sys.columns c
            INNER JOIN sys.tables t ON c.object_id = t.object_id
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE s.name = 'logcollection'
                AND t.name = @TableName
                AND c.name = @ColumnName;";

        using var checkCommand = new SqlCommand(checkColumnSql, connection, transaction);
        checkCommand.Parameters.AddWithValue("@TableName", logCollection.TableName);
        checkCommand.Parameters.AddWithValue("@ColumnName", logAttribute.SqlColumnName);

        var count = (int)(await checkCommand.ExecuteScalarAsync() ?? 0);
        return count > 0;
    }

    internal async Task<List<string>> ListIndexesOfAttributeAsync(LogCollection logCollection, LogAttribute logAttribute, SqlConnection connection, SqlTransaction transaction)
    {
        var getIndexesSql = @"
            SELECT i.name
            FROM sys.indexes i
            INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
            INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
            INNER JOIN sys.tables t ON i.object_id = t.object_id
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE s.name = 'logcollection'
                AND t.name = @TableName
                AND c.name = @ColumnName
                AND i.is_primary_key = 0;";

        var indexNames = new List<string>();
        using var getIndexesCommand = new SqlCommand(getIndexesSql, connection, transaction);
        getIndexesCommand.Parameters.AddWithValue("@TableName", logCollection.TableName);
        getIndexesCommand.Parameters.AddWithValue("@ColumnName", logAttribute.SqlColumnName);

        using var reader = await getIndexesCommand.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            indexNames.Add(reader.GetString(0));
        }

        return indexNames;
    }

    public async IAsyncEnumerable<LogAttribute> ListAttributesAsync()
    {
        var sql = @"
            SELECT [ID], [LogCollectionID], [Name], [SqlColumnName], [AttributeTypeID], [ExtractionStyleID], [ExtractionExpression]
            FROM [dbo].[LogCollectionAttribute];";

        using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            yield return new LogAttribute(
                logCollectionID: reader.GetInt64(1),
                name: reader.GetString(2),
                sqlColumnName: reader.GetString(3),
                attributeTypeID: reader.GetString(4),
                extractionStyleID: reader.GetString(5),
                extractionExpression: reader.GetString(6))
            {
                ID = reader.GetInt64(0)
            };
        }
    }

    public async IAsyncEnumerable<LogAttribute> ListAttributesOfCollectionAsync(LogCollection logCollection)
    {
        var sql = @"
            SELECT [ID], [LogCollectionID], [Name], [SqlColumnName], [AttributeTypeID], [ExtractionStyleID], [ExtractionExpression]
            FROM [dbo].[LogCollectionAttribute]
            WHERE [LogCollectionID] = @LogCollectionID
            ORDER BY [ID] ASC;";

        using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@LogCollectionID", logCollection.ID);

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var logAttribute = new LogAttribute(
                logCollectionID: reader.GetInt64(1),
                name: reader.GetString(2),
                sqlColumnName: reader.GetString(3),
                attributeTypeID: reader.GetString(4),
                extractionStyleID: reader.GetString(5),
                extractionExpression: reader.GetString(6)
            );

            logAttribute.ID = reader.GetInt64(0);

            yield return logAttribute;
        }
    }
}
