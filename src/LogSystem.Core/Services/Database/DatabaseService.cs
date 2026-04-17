namespace LogSystem.Core.Services.Database;

public class DatabaseService
{
    private readonly DatabaseConfig DatabaseConfig;

    public DatabaseService(DatabaseConfig databaseConfig)
    {
        DatabaseConfig = databaseConfig;
    }

    public async Task CreateAttributeAsync(System system, LogAttribute logAttribute)
    {
        // TODO
        // Insert into table "SystemLogAttribute"
        // Then, execute ALTER TABLE on System.TableName to include the added column
        // Created column is always nullable
        // Add an index on the column with NOT NULL filter
        // If created column is for text/varchar, both the column and the index should be case-insensitive
    }

    public async Task DeleteAttributeAsync(System system, LogAttribute logAttribute)
    {
        // TODO
        // Delete from table "SystemLogAttribute"
        // Then, execute ALTER TABLE on System.TableName to remove the added column (if it doesn't exist)
        // Check if indexes exist on the deleted column, and delete them if they exist
    }

    public async Task SaveSystemAsync(System system)
    {
        // TODO
        // Persist the System into database - can be either insert (ID=0) or update (ID<>0)
        // Change the System.ID if inserted
        // SystemID is auto-incremented by the database
    }

    public async Task SaveLogsAsync(System system, IEnumerable<Log> logs)
    {
        // TODO
        // Execute BULK INSERT into System.TableName table
        // The columns are defined in the "logs" object
        // Fixed columns are attributes such as "ID" (auto-increment) and "ValidUntilUtc"
        // Dynamic columns are defined in "Log.Attributes", where the "Key" is the column name, and "Value" is the column value
        // Always check for nulls to insert DBNull.Value
    }

    public async IAsyncEnumerable<Log> QueryLogsAsync(System system, IEnumerable<LogAttribute> attributes, IEnumerable<LogFilter> filters)
    {
        // TODO
        // Execute SELECT from table contained in System.TableName
        // Filters should be applied and parsed based on the filters and attributes available to the system
        // Fixed columns are attributes such as "ID" (auto-increment) and "ValidUntilUtc"
        // Dynamic columns are defined in "Log.Attributes", where the "Key" is the column name, and "Value" is the column value
    }
}