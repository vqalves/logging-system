using System.Data;
using Microsoft.Data.SqlClient;

namespace LogSystem.Core.Services.Database;

public class DatabaseService
{
    private readonly DatabaseConfig DatabaseConfig;

    public DatabaseService(DatabaseConfig databaseConfig)
    {
        DatabaseConfig = databaseConfig;
    }

    public async Task CreateAttributeAsync(LogCollection logCollection, LogAttribute logAttribute)
    {
        // Get the SQL data type from the AttributeType
        var attributeType = AttributeType.Parse(logAttribute.AttributeTypeID);
        var sqlDataType = attributeType!.SqlDataType;

        // For VARCHAR columns, add collation
        if (attributeType == AttributeType.Text)
            sqlDataType += " COLLATE SQL_Latin1_General_CP1_CI_AS";

        using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
        await connection.OpenAsync();

        // Wrap insert + ALTER TABLE in transaction for atomicity
        using var transaction = connection.BeginTransaction();
        try
        {
            // Insert into LogCollectionAttribute table and retrieve the new ID
            var insertSql = @"
                INSERT INTO [dbo].[LogCollectionAttribute] ([LogCollectionID], [Name], [SqlColumnName], [AttributeTypeID], [ExtractionStyleID], [ExtractionExpression])
                VALUES (@LogCollectionID, @Name, @SqlColumnName, @AttributeTypeID, @ExtractionStyleID, @ExtractionExpression);
                SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

            using (var insertCommand = new SqlCommand(insertSql, connection, transaction))
            {
                insertCommand.Parameters.AddWithValue("@LogCollectionID", logAttribute.LogCollectionID);
                insertCommand.Parameters.AddWithValue("@Name", logAttribute.Name);
                insertCommand.Parameters.AddWithValue("@SqlColumnName", logAttribute.SqlColumnName);
                insertCommand.Parameters.AddWithValue("@AttributeTypeID", logAttribute.AttributeTypeID);
                insertCommand.Parameters.AddWithValue("@ExtractionStyleID", logAttribute.ExtractionStyleID);
                insertCommand.Parameters.AddWithValue("@ExtractionExpression", logAttribute.ExtractionExpression);

                var newId = await insertCommand.ExecuteScalarAsync();
                logAttribute.ID = (long)(newId ?? throw new InvalidOperationException("Failed to retrieve new LogAttribute ID from database."));
            }

            // Execute ALTER TABLE to add the column
            var alterTableSql = $@"
                ALTER TABLE [dbo].[{logCollection.TableName}]
                ADD [{logAttribute.SqlColumnName}] {sqlDataType} NULL;";

            using (var alterTableCommand = new SqlCommand(alterTableSql, connection, transaction))
                await alterTableCommand.ExecuteNonQueryAsync();

            // Create filtered index on the column
            var createIndexSql = $@"
                CREATE INDEX [IX_{logCollection.TableName}_{logAttribute.SqlColumnName}]
                ON [dbo].[{logCollection.TableName}] ([{logAttribute.SqlColumnName}])
                WHERE [{logAttribute.SqlColumnName}] IS NOT NULL;";

            using (var createIndexCommand = new SqlCommand(createIndexSql, connection, transaction))
                await createIndexCommand.ExecuteNonQueryAsync();

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
            var deleteSql = @"
                DELETE FROM [dbo].[LogCollectionAttribute]
                WHERE [ID] = @ID;";

            using (var deleteCommand = new SqlCommand(deleteSql, connection, transaction))
            {
                deleteCommand.Parameters.AddWithValue("@ID", logAttribute.ID);
                await deleteCommand.ExecuteNonQueryAsync();
            }

            bool columnExists = await DoesColumnExistsAsync(logCollection, logAttribute, connection, transaction);

            if (columnExists)
            {
                List<string> indexNames = await ListIndexesOfAttributeAsync(logCollection, logAttribute, connection, transaction);

                foreach (var indexName in indexNames)
                    await DropIndexAsync(logCollection, connection, transaction, indexName);

                await DropTableColumnAsync(logCollection, logAttribute, connection, transaction);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static async Task DropTableColumnAsync(LogCollection logCollection, LogAttribute logAttribute, SqlConnection connection, SqlTransaction transaction)
    {
        var alterTableSql = $@"ALTER TABLE [dbo].[{logCollection.TableName}] DROP COLUMN [{logAttribute.SqlColumnName}];";

        using (var alterTableCommand = new SqlCommand(alterTableSql, connection, transaction))
            await alterTableCommand.ExecuteNonQueryAsync();
    }

    private static async Task DropIndexAsync(LogCollection logCollection, SqlConnection connection, SqlTransaction transaction, string indexName)
    {
        var dropIndexSql = $@"DROP INDEX [{indexName}] ON [dbo].[{logCollection.TableName}];";

        using var dropIndexCommand = new SqlCommand(dropIndexSql, connection, transaction);
        await dropIndexCommand.ExecuteNonQueryAsync();
    }

    private static async Task<List<string>> ListIndexesOfAttributeAsync(LogCollection logCollection, LogAttribute logAttribute, SqlConnection connection, SqlTransaction transaction)
    {
        // Drop associated indexes using sys.indexes and sys.index_columns catalog views
        var getIndexesSql = @"
            SELECT i.name
            FROM sys.indexes i
            INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
            INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
            INNER JOIN sys.tables t ON i.object_id = t.object_id
            WHERE t.name = @TableName
                AND c.name = @ColumnName
                AND i.is_primary_key = 0;";

        var indexNames = new List<string>();
        using (var getIndexesCommand = new SqlCommand(getIndexesSql, connection, transaction))
        {
            getIndexesCommand.Parameters.AddWithValue("@TableName", logCollection.TableName);
            getIndexesCommand.Parameters.AddWithValue("@ColumnName", logAttribute.SqlColumnName);

            using var reader = await getIndexesCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                indexNames.Add(reader.GetString(0));
            }
        }

        return indexNames;
    }

    private static async Task<bool> DoesColumnExistsAsync(LogCollection logCollection, LogAttribute logAttribute, SqlConnection connection, SqlTransaction transaction)
    {
        // Check if column exists in the table using sys.columns catalog view
        var checkColumnSql = @"
            SELECT COUNT(*)
            FROM sys.columns c
            INNER JOIN sys.tables t ON c.object_id = t.object_id
            WHERE t.name = @TableName
                AND c.name = @ColumnName;";

        bool columnExists;
        using (var checkCommand = new SqlCommand(checkColumnSql, connection, transaction))
        {
            checkCommand.Parameters.AddWithValue("@TableName", logCollection.TableName);
            checkCommand.Parameters.AddWithValue("@ColumnName", logAttribute.SqlColumnName);
            var count = (int)(await checkCommand.ExecuteScalarAsync() ?? 0);
            columnExists = count > 0;
        }

        return columnExists;
    }

    public async Task<LogCollection?> GetLogCollectionByNameAsync(string name)
    {
        using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT [ID], [Name], [TableName], [LogDurationHours]
            FROM [dbo].[LogCollection]
            WHERE [Name] = @Name;";

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Name", name);

        using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow);

        if (await reader.ReadAsync())
        {
            var logCollection = new LogCollection(
                name: reader.GetString(1),
                tableName: reader.GetString(2),
                logDurationHours: reader.GetInt64(3)
            );
            logCollection.ID = reader.GetInt64(0);
            return logCollection;
        }

        return null;
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
                var insertSql = @"
                    INSERT INTO [dbo].[LogCollection] ([Name], [TableName], [LogDurationHours])
                    VALUES (@Name, @TableName, @LogDurationHours);
                    SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

                using (var insertCommand = new SqlCommand(insertSql, connection, transaction))
                {
                    insertCommand.Parameters.AddWithValue("@Name", logCollection.Name);
                    insertCommand.Parameters.AddWithValue("@TableName", logCollection.TableName);
                    insertCommand.Parameters.AddWithValue("@LogDurationHours", logCollection.LogDurationHours);

                    var newId = await insertCommand.ExecuteScalarAsync();
                    logCollection.ID = (long)(newId ?? throw new InvalidOperationException("Failed to retrieve new LogCollection ID from database."));
                }

                // Create dynamic log table with fixed columns
                var createTableSql = $@"
                    CREATE TABLE [dbo].[{logCollection.TableName}]
                    (
                        [ID] BIGINT IDENTITY(1,1) NOT NULL,
                        [SourceFileIndex] INT NOT NULL,
                        [SourceFileName] VARCHAR(100) NOT NULL,
                        [ValidUntilUtc] DATETIME2 NOT NULL,
                        CONSTRAINT [PK_{logCollection.TableName}] PRIMARY KEY CLUSTERED ([ID] ASC)
                    );";

                using (var createTableCommand = new SqlCommand(createTableSql, connection, transaction))
                    await createTableCommand.ExecuteNonQueryAsync();

                // Add index on ValidUntilUtc for cleanup queries
                var createIndexSql = $@"
                    CREATE NONCLUSTERED INDEX [IX_{logCollection.TableName}_ValidUntilUtc]
                    ON [dbo].[{logCollection.TableName}] ([ValidUntilUtc] ASC);";

                using (var createIndexCommand = new SqlCommand(createIndexSql, connection, transaction))
                    await createIndexCommand.ExecuteNonQueryAsync();

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
            var updateSql = @"
                UPDATE [dbo].[LogCollection]
                SET [Name] = @Name, [LogDurationHours] = @LogDurationHours
                WHERE [ID] = @ID;";

            using var updateCommand = new SqlCommand(updateSql, connection);
            updateCommand.Parameters.AddWithValue("@Name", logCollection.Name);
            updateCommand.Parameters.AddWithValue("@LogDurationHours", logCollection.LogDurationHours);
            updateCommand.Parameters.AddWithValue("@ID", logCollection.ID);

            await updateCommand.ExecuteNonQueryAsync();
        }
    }

    public async Task SaveLogsAsync(LogCollection logCollection, IEnumerable<Log> logs)
    {
        // Materialize the collection to avoid multiple enumeration
        var logsList = logs as IList<Log> ?? logs.ToList();

        if (logsList.Count == 0)
            return; // Nothing to save

        using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
        await connection.OpenAsync();

        if (logsList.Count < 10)
        {
            await SaveLogsWithBatchInsertAsync(logCollection, logsList, connection);
        }
        else
        {
            await SaveLogsWithBulkCopyAsync(logCollection, logsList, connection);
        }
    }

    private async Task SaveLogsWithBatchInsertAsync(LogCollection logCollection, IList<Log> logs, SqlConnection connection)
    {
        foreach (var log in logs)
        {
            // Build column names and parameter names
            var columnNames = new List<string> { "[SourceFileIndex]", "[SourceFileName]", "[ValidUntilUtc]" };
            var parameterNames = new List<string> { "@SourceFileIndex", "@SourceFileName", "@ValidUntilUtc" };

            // Add dynamic columns from attributes
            if (log.Attributes != null)
            {
                foreach (var attribute in log.Attributes)
                {
                    columnNames.Add($"[{attribute.Key}]");
                    parameterNames.Add($"@{attribute.Key}");
                }
            }

            // Build INSERT statement
            var insertSql = $@"
                INSERT INTO [dbo].[{logCollection.TableName}] ({string.Join(", ", columnNames)})
                VALUES ({string.Join(", ", parameterNames)});";

            using var command = new SqlCommand(insertSql, connection);

            // Add fixed parameters
            command.Parameters.AddWithValue("@SourceFileIndex", log.SourceFileIndex);
            command.Parameters.AddWithValue("@SourceFileName", log.SourceFileName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ValidUntilUtc", log.ValidUntilUtc);

            // Add dynamic parameters from attributes
            if (log.Attributes != null)
                foreach (var attribute in log.Attributes)
                    command.Parameters.AddWithValue($"@{attribute.Key}", attribute.Value ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }
    }

    private async Task SaveLogsWithBulkCopyAsync(LogCollection logCollection, IList<Log> logs, SqlConnection connection)
    {
        // Create DataTable with schema matching the target table
        var dataTable = new DataTable();

        // Add fixed columns
        dataTable.Columns.Add("SourceFileIndex", typeof(int));
        dataTable.Columns.Add("SourceFileName", typeof(string));
        dataTable.Columns.Add("ValidUntilUtc", typeof(DateTime));

        // Add dynamic columns from first log's attributes
        var firstLog = logs[0];
        var dynamicColumns = new List<string>();
        if (firstLog.Attributes != null)
        {
            foreach (var attribute in firstLog.Attributes)
            {
                dataTable.Columns.Add(attribute.Key, typeof(object));
                dynamicColumns.Add(attribute.Key);
            }
        }

        // Populate rows from logs collection
        foreach (var log in logs)
        {
            var row = dataTable.NewRow();

            // Set fixed column values
            row["SourceFileIndex"] = log.SourceFileIndex;
            row["SourceFileName"] = log.SourceFileName ?? (object)DBNull.Value;
            row["ValidUntilUtc"] = log.ValidUntilUtc;

            // Set dynamic column values
            foreach (var columnName in dynamicColumns)
            {
                if (log.Attributes != null && log.Attributes.TryGetValue(columnName, out var value))
                    row[columnName] = value ?? DBNull.Value;
                else
                    row[columnName] = DBNull.Value;
            }

            dataTable.Rows.Add(row);
        }

        // Use SqlBulkCopy to write to server
        using var bulkCopy = new SqlBulkCopy(connection)
        {
            DestinationTableName = $"[dbo].[{logCollection.TableName}]",
            BatchSize = 1000
        };

        // Map columns explicitly
        bulkCopy.ColumnMappings.Add("SourceFileIndex", "SourceFileIndex");
        bulkCopy.ColumnMappings.Add("SourceFileName", "SourceFileName");
        bulkCopy.ColumnMappings.Add("ValidUntilUtc", "ValidUntilUtc");

        foreach (var columnName in dynamicColumns)
            bulkCopy.ColumnMappings.Add(columnName, columnName);

        await bulkCopy.WriteToServerAsync(dataTable);
    }

    public async IAsyncEnumerable<LogAttribute> ListLogAttributesAsync()
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

    public async IAsyncEnumerable<LogCollection> ListLogCollectionsAsync()
    {
        var sql = @"
            SELECT [ID], [Name], [TableName], [LogDurationHours]
            FROM [dbo].[LogCollection];";

        using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var logCollection = new LogCollection(
                name: reader.GetString(1),
                tableName: reader.GetString(2),
                logDurationHours: reader.GetInt64(3));
            logCollection.ID = reader.GetInt64(0);
            yield return logCollection;
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

        using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

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

            // Set the ID property using reflection or object initializer workaround
            logAttribute.ID = reader.GetInt64(0);

            yield return logAttribute;
        }
    }

    public async IAsyncEnumerable<Log> QueryLogsAsync(LogCollection logCollection, IEnumerable<LogAttribute> attributes, IEnumerable<LogFilter> filters)
    {
        // Materialize collections to avoid multiple enumeration
        var attributesList = attributes as IList<LogAttribute> ?? attributes.ToList();
        var filtersList = filters as IList<LogFilter> ?? filters.ToList();

        // Build column list (fixed + dynamic)
        var columnNames = new List<string>
        {
            "[ID]",
            "[SourceFileIndex]",
            "[SourceFileName]",
            "[ValidUntilUtc]"
        };

        // Add dynamic columns from attributes
        foreach (var attribute in attributesList)
            columnNames.Add($"[{attribute.SqlColumnName}]");

        // Build WHERE clause
        var whereClauses = new List<string>();
        var parameters = new List<SqlParameter>();
        var paramIndex = 0;

        foreach (var filter in filtersList)
        {
            // Find the attribute for this filter
            var attribute = attributesList.FirstOrDefault(a => a.ID == filter.LogAttributeID);
            var columnName = $"[{attribute!.SqlColumnName}]";
            var filterOperator = FilterOperator.Parse(filter.Operator);

            // Build WHERE clause based on operator
            if (filterOperator == FilterOperator.IsNull)
                whereClauses.Add($"{columnName} IS NULL");

            else if (filterOperator == FilterOperator.IsNotNull)
                whereClauses.Add($"{columnName} IS NOT NULL");

            else if (filterOperator == FilterOperator.Contains)
            {
                var paramName = $"@p{paramIndex++}";
                whereClauses.Add($"{columnName} LIKE '%' + {paramName} + '%'");
                parameters.Add(new SqlParameter(paramName, ConvertFilterValue(filter.Value, attribute)));
            }

            else if (filterOperator == FilterOperator.StartsWith)
            {
                var paramName = $"@p{paramIndex++}";
                whereClauses.Add($"{columnName} LIKE {paramName} + '%'");
                parameters.Add(new SqlParameter(paramName, ConvertFilterValue(filter.Value, attribute)));
            }

            else if (filterOperator == FilterOperator.EndsWith)
            {
                var paramName = $"@p{paramIndex++}";
                whereClauses.Add($"{columnName} LIKE '%' + {paramName}");
                parameters.Add(new SqlParameter(paramName, ConvertFilterValue(filter.Value, attribute)));
            }
            else
            {
                // Comparison operators: =, !=, <, <=, >, >=
                var paramName = $"@p{paramIndex++}";
                whereClauses.Add($"{columnName} {filterOperator.Value} {paramName}");
                parameters.Add(new SqlParameter(paramName, ConvertFilterValue(filter.Value, attribute)));
            }
        }

        // Build final SQL query
        var sql = $"SELECT {string.Join(", ", columnNames)} FROM [dbo].[{logCollection.TableName}]";
        if (whereClauses.Count > 0)
            sql += $" WHERE {string.Join(" AND ", whereClauses)}";

        // Execute query
        using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters.ToArray());

        using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

        // Map column indices for dynamic columns
        var dynamicColumnIndices = new Dictionary<string, int>();
        for (int i = 0; i < attributesList.Count; i++)
        {
            var attribute = attributesList[i];
            dynamicColumnIndices[attribute.SqlColumnName] = 4 + i;
        }

        // Read and yield results
        while (await reader.ReadAsync())
        {
            var log = new Log
            {
                ID = reader.GetInt64(0),
                SourceFileIndex = reader.GetInt32(1),
                SourceFileName = reader.GetString(2),
                ValidUntilUtc = reader.GetDateTime(3),
                Attributes = new Dictionary<string, object>()
            };

            // Read dynamic columns
            foreach (var attribute in attributesList)
            {
                var columnIndex = dynamicColumnIndices[attribute.SqlColumnName];

                if (!reader.IsDBNull(columnIndex))
                {
                    var attributeType = attribute.GetAttributeType();
                    object value;

                    if (attributeType == AttributeType.Text)
                        value = reader.GetString(columnIndex);

                    else if (attributeType == AttributeType.Integer)
                        value = reader.GetInt32(columnIndex);

                    else if (attributeType == AttributeType.DateTime)
                        value = reader.GetDateTime(columnIndex);

                    else
                        value = reader.GetValue(columnIndex);

                    log.Attributes[attribute.SqlColumnName] = value;
                }
            }

            yield return log;
        }
    }

    private object ConvertFilterValue(object? value, LogAttribute attribute)
    {
        if (value == null)
        {
            return DBNull.Value;
        }

        var attributeType = attribute.GetAttributeType();

        if (attributeType == AttributeType.Text)
        {
            return value.ToString() ?? string.Empty;
        }
        else if (attributeType == AttributeType.Integer)
        {
            if (value is int intValue)
            {
                return intValue;
            }
            if (int.TryParse(value.ToString(), out var parsedInt))
            {
                return parsedInt;
            }
            throw new ArgumentException($"Cannot convert value '{value}' to integer for attribute '{attribute.Name}'.");
        }
        else if (attributeType == AttributeType.DateTime)
        {
            if (value is DateTime dateTimeValue)
            {
                return dateTimeValue;
            }
            if (DateTime.TryParse(value.ToString(), out var parsedDateTime))
            {
                return parsedDateTime;
            }
            throw new ArgumentException($"Cannot convert value '{value}' to datetime for attribute '{attribute.Name}'.");
        }

        return value;
    }
}