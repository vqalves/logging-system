using System.Data;
using Microsoft.Data.SqlClient;

namespace LogSystem.Core.Services.Database;

public class LogDataService
{
    private readonly DatabaseConfig DatabaseConfig;

    public LogDataService(DatabaseConfig databaseConfig)
    {
        DatabaseConfig = databaseConfig;
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
            await SaveLogsBatchAsync(logCollection, logsList, connection);
        }
        else
        {
            await SaveLogsBulkAsync(logCollection, logsList, connection);
        }
    }

    public async Task<int> DeleteExpiredLogsAsync(LogCollection logCollection, int maxRows)
    {
        using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
        await connection.OpenAsync();

        var deleteSql = $@"
            DELETE TOP(@MaxRows)
            FROM [logcollection].[{logCollection.TableName}]
            WHERE [ValidUntilUtc] < @CutoffTime;";

        using var command = new SqlCommand(deleteSql, connection);
        command.Parameters.AddWithValue("@MaxRows", maxRows);
        command.Parameters.AddWithValue("@CutoffTime", DateTime.UtcNow);

        var rowsDeleted = await command.ExecuteNonQueryAsync();
        return rowsDeleted;
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
                whereClauses.Add($"{columnName} {filterOperator!.Value} {paramName}");
                parameters.Add(new SqlParameter(paramName, ConvertFilterValue(filter.Value, attribute)));
            }
        }

        // Build final SQL query
        var sql = $"SELECT {string.Join(", ", columnNames)} FROM [logcollection].[{logCollection.TableName}]";
        if (whereClauses.Count > 0)
            sql += $" WHERE {string.Join(" AND ", whereClauses)}";

        // Execute query
        using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters.ToArray());

        using var reader = await command.ExecuteReaderAsync();

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

    internal async Task SaveLogsBatchAsync(LogCollection logCollection, IList<Log> logs, SqlConnection connection)
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
                INSERT INTO [logcollection].[{logCollection.TableName}] ({string.Join(", ", columnNames)})
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

    internal async Task SaveLogsBulkAsync(LogCollection logCollection, IList<Log> logs, SqlConnection connection)
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
            DestinationTableName = $"[logcollection].[{logCollection.TableName}]",
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
