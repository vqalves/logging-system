using System.Text.RegularExpressions;

namespace LogSystem.Core.Services.Database;

public class LogCollection
{
    public long ID { get; set; }
    public string Name { get; set; }
    public string ClientId { get; set; }
    public string TableName { get; }
    public long LogDurationHours { get; set; }

    public LogCollection(string name, string clientId, string tableName, long logDurationHours)
    {
        // TODO: Create static methods to encapsulate the validations belows
        // Use those static methods on the CreateOrUpdateLogCollection validation methods instead of duplicating the logic
        // Update the UI for create/edit logcollection to inform the user about the allowed characters.

        // Validate ClientId (alphanumeric + hyphen + underscore + dot)
        if (!Regex.IsMatch(clientId, @"^[a-zA-Z0-9_.-]+$"))
        {
            throw new ArgumentException("ClientId must contain only alphanumeric characters, hyphens, underscores, and dots.", nameof(clientId));
        }

        // Validate TableName to prevent SQL injection (alphanumeric + underscore only)
        if (!Regex.IsMatch(tableName, @"^[a-zA-Z0-9_]+$"))
        {
            throw new ArgumentException("TableName must contain only alphanumeric characters and underscores.", nameof(tableName));
        }

        Name = name;
        ClientId = clientId;
        TableName = tableName;
        LogDurationHours = logDurationHours;
    }
}
