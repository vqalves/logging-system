namespace LogSystem.Core.Services.Database;

public class LogExtractionService
{
    public Log Extract(LogCollection logCollection, IEnumerable<LogAttribute> attributes, string logContent, int sourceFileIndex, string sourceFileName)
    {
        // Create new Log instance with source information and calculated expiration
        var log = new Log
        {
            SourceFileIndex = sourceFileIndex,
            SourceFileName = sourceFileName,
            ValidUntilUtc = DateTime.UtcNow.AddHours(logCollection.LogDurationHours),
            Attributes = new Dictionary<string, object>()
        };

        // Parse content once if JSON format is needed
        global::System.Text.Json.JsonDocument? jsonDocument = null;
        bool needsJsonParsing = attributes.Any(a =>
            a.ExtractionStyleID?.Equals("json", StringComparison.InvariantCultureIgnoreCase) == true);

        try
        {
            if (needsJsonParsing)
            {
                try
                {
                    jsonDocument = global::System.Text.Json.JsonDocument.Parse(logContent);
                }
                catch (global::System.Text.Json.JsonException)
                {
                    // If JSON parsing fails, continue but attributes will fail to extract
                    // This allows graceful handling of malformed JSON
                }
            }

            // Iterate through attributes and extract values
            foreach (var attribute in attributes)
            {
                object? extractedValue = null;

                try
                {
                    // Determine extraction style and delegate to appropriate method
                    if (string.Equals(attribute.ExtractionStyleID, "json", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (jsonDocument is not null)
                        {
                            extractedValue = ExtractionStyle.JSON.Extract(jsonDocument, attribute.ExtractionExpression);
                        }
                    }
                    // Future extraction styles (regex, xpath, etc.) can be added here

                    // Handle type conversion based on AttributeType
                    if (extractedValue != null)
                    {
                        var attributeType = attribute.GetAttributeType();
                        extractedValue = ConvertToAttributeType(extractedValue, attributeType);
                    }

                    // Populate Log.Attributes dictionary with SqlColumnName as key
                    log.Attributes[attribute.SqlColumnName] = extractedValue!;
                }
                catch (Exception)
                {
                    // Log extraction failure gracefully - set attribute to null
                    // In production, consider logging the exception for debugging
                    log.Attributes[attribute.SqlColumnName] = null!;
                }
            }
        }
        finally
        {
            // Ensure JsonDocument is properly disposed
            jsonDocument?.Dispose();
        }

        return log;
    }

    /// <summary>
    /// Converts an extracted value to the appropriate type based on the AttributeType.
    /// </summary>
    private object? ConvertToAttributeType(object value, AttributeType attributeType)
    {
        if (value == null)
            return null;

        // If already the correct type, return as-is
        if (attributeType == AttributeType.Text && value is string)
            return value;

        if (attributeType == AttributeType.Integer && value is int)
            return value;

        if (attributeType == AttributeType.DateTime && value is DateTime)
            return value;

        // Handle type conversions
        try
        {
            if (attributeType == AttributeType.Text)
            {
                return value.ToString();
            }
            else if (attributeType == AttributeType.Integer)
            {
                // Try direct conversion first, then parse string
                if (value is long longValue)
                    return (int)longValue;
                if (value is double doubleValue)
                    return (int)doubleValue;

                return Convert.ToInt32(value);
            }
            else if (attributeType == AttributeType.DateTime)
            {
                // Try direct conversion first, then parse string
                if (value is string strValue)
                    return DateTime.Parse(strValue);

                return Convert.ToDateTime(value);
            }

            // If no specific conversion, return original value
            return value;
        }
        catch
        {
            // If conversion fails, return null
            return null;
        }
    }
}
