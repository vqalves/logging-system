using System.Text.RegularExpressions;

namespace LogSystem.Core.Services.Database;

public class LogCollection
{
    private static readonly Regex ClientIdRegex = new(@"^[a-zA-Z0-9_.-]+$", RegexOptions.Compiled);
    private static readonly Regex TableNameRegex = new(@"^[a-zA-Z0-9_]+$", RegexOptions.Compiled);

    public long ID { get; set; }
    public string Name { get; set; }
    public string ClientId { get; set; }
    public string TableName { get; }
    public int LogDurationDays { get; set; }

    public LogCollection(string name, string clientId, string tableName, int logDurationDays)
    {
        if (!TryValidateClientId(clientId, out var errorMessage))
            throw new ArgumentException(errorMessage, nameof(clientId));

        if (!TryValidateTableName(tableName, out errorMessage))
            throw new ArgumentException(errorMessage, nameof(tableName));

        Name = name;
        ClientId = clientId;
        TableName = tableName;
        LogDurationDays = logDurationDays;
    }

    public static bool TryValidateClientId(string clientId, out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            errorMessage = "ClientId is required.";
            return false;
        }

        if (!ClientIdRegex.IsMatch(clientId))
        {
            errorMessage = "ClientId must contain only alphanumeric characters, hyphens, underscores, and dots.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    public static bool TryValidateTableName(string tableName, out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            errorMessage = "TableName is required.";
            return false;
        }

        if (!TableNameRegex.IsMatch(tableName))
        {
            errorMessage = "TableName must contain only alphanumeric characters and underscores.";
            return false;
        }

        errorMessage = null;
        return true;
    }
}
