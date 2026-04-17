using System.Text.RegularExpressions;

namespace LogSystem.Core.Services.Database;

public class LogCollection
{
    public long ID { get; set; }
    public string Name { get; }
    public string TableName { get; }
    public long LogDurationHours { get; set; }

    public LogCollection(string name, string tableName, long logDurationHours)
    {
        // Validate TableName to prevent SQL injection (alphanumeric + underscore only)
        if (!Regex.IsMatch(tableName, @"^[a-zA-Z0-9_]+$"))
        {
            throw new ArgumentException("TableName must contain only alphanumeric characters and underscores.", nameof(tableName));
        }

        Name = name;
        TableName = tableName;
        LogDurationHours = logDurationHours;
    }
}
