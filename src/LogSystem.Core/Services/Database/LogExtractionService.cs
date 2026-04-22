using System.Dynamic;
using System.Text.Json;

namespace LogSystem.Core.Services.Database;

public class LogExtractionService
{
    public Log Extract(LogCollection logCollection, IEnumerable<LogAttribute> attributes, Func<JsonDocument?> contentAsJsonDocument)
    {
        // Create new Log instance with source information and calculated expiration
        var log = new Log
        {
            ValidUntilUtc = DateTime.UtcNow.AddHours(logCollection.LogDurationHours),
            Attributes = new Dictionary<string, object>()
        };

        // Parse content once if JSON format is needed
        JsonDocument? jsonDocument = null;
        bool needsJsonParsing = attributes.Any(a => a.HasExtractionStyle(ExtractionStyle.JSON));

        if (needsJsonParsing)
            jsonDocument = contentAsJsonDocument();

        // Iterate through attributes and extract values
        foreach (var attribute in attributes)
        {
            object? extractedValue = null;

            try
            {
                // Determine extraction style and delegate to appropriate method
                if (attribute.HasExtractionStyle(ExtractionStyle.JSON))
                    if (jsonDocument is not null)
                        extractedValue = ExtractionStyle.JSON.Extract(jsonDocument, attribute.ExtractionExpression);

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
        if (attributeType == AttributeType.Text)
        {
            if(value is string)
                return value;

            return value.ToString();
        }

        if (attributeType == AttributeType.Integer)
        {
            if(value is int)
                return value;

            if (value is long longValue)
                return (int)longValue;

            if (value is double doubleValue)
                return (int)doubleValue;

            if(int.TryParse(value.ToString(), out var intValue))
                return intValue;

            try
            {
                return Convert.ToInt32(value);
            }            
            catch
            {
                return null;
            }
        }

        if (attributeType == AttributeType.DateTime)
        {
            if (value is DateTime)
                return value;

            if (value is string strValue && DateTime.TryParse(strValue, out var dateTimeValue))
                return dateTimeValue;

            try
            {
                return Convert.ToDateTime(value);
            }
            catch
            {
                return null;
            }
        }

        if (attributeType == AttributeType.Boolean)
        {
            if (value is bool)
                return value;

            if (value is string boolStrValue && bool.TryParse(boolStrValue, out var boolValue))
                return boolValue;

            try
            {
                return Convert.ToBoolean(value);
            }
            catch
            {
                return null;
            }
        }

        if (attributeType == AttributeType.Decimal)
        {
            if (value is decimal)
                return value;

            if (value is double doubleValue)
                return (decimal)doubleValue;

            if (value is float floatValue)
                return (decimal)floatValue;

            if (value is int intValue)
                return (decimal)intValue;

            if (value is long longValue)
                return (decimal)longValue;

            if (value is string strValue && decimal.TryParse(strValue, out var decimalValue))
                return decimalValue;

            try
            {
                return Convert.ToDecimal(value);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }
}
