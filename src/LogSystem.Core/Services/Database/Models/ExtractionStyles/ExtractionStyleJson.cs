using System.Text.Json;

namespace LogSystem.Core.Services.Database;

public class ExtractionStyleJson : ExtractionStyle
{
    public ExtractionStyleJson() : base("json")
    {

    }

    /// <summary>
    /// Extracts a value from JSON content using a JSONPath expression.
    /// </summary>
    /// <param name="content">JsonDocument containing the parsed JSON content.
    /// Using JsonDocument instead of string allows LogExtractionService to parse once and reuse across multiple attributes.</param>
    /// <param name="expression">JSONPath expression (e.g., "$.person.name" or "person.name")</param>
    /// <returns>Extracted value as object (string, int, DateTime, etc.) or null if path not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when content or expression is null</exception>
    /// <exception cref="ArgumentException">Thrown when content is not a JsonDocument</exception>
    public object? Extract(JsonDocument content, string expression)
    {
        // Navigate the JSON structure using the JSONPath expression
        JsonElement? currentElement = NavigateJsonPath(content.RootElement, expression);

        if (currentElement == null)
            return null;

        // Extract the value with appropriate type conversion
        return ExtractValue(currentElement.Value);
    }

    /// <summary>
    /// Navigates a JSON structure using a JSONPath expression.
    /// Supports basic dot notation (e.g., "$.person.name" or "person.name").
    /// </summary>
    private JsonElement? NavigateJsonPath(JsonElement rootElement, string expression)
    {
        // Remove leading "$." or "$[" if present
        string path = expression.TrimStart();
        if (path.StartsWith("$."))
            path = path.Substring(2);
        else if (path.StartsWith("$"))
            path = path.Substring(1);

        // Split by dots to get path segments
        string[] segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries);

        JsonElement currentElement = rootElement;

        foreach (string segment in segments)
        {
            // Handle array indexing (e.g., "items[0]")
            if (segment.Contains('[') && segment.Contains(']'))
            {
                int bracketIndex = segment.IndexOf('[');
                string propertyName = segment.Substring(0, bracketIndex);
                string indexPart = segment.Substring(bracketIndex + 1, segment.IndexOf(']') - bracketIndex - 1);

                // Navigate to property if it exists
                if (!string.IsNullOrEmpty(propertyName))
                {
                    if (currentElement.ValueKind != JsonValueKind.Object ||
                        !currentElement.TryGetProperty(propertyName, out currentElement))
                        return null;
                }

                // Navigate to array index
                if (int.TryParse(indexPart, out int index))
                {
                    if (currentElement.ValueKind != JsonValueKind.Array)
                        return null;

                    int currentIndex = 0;
                    foreach (JsonElement arrayElement in currentElement.EnumerateArray())
                    {
                        if (currentIndex == index)
                        {
                            currentElement = arrayElement;
                            break;
                        }
                        currentIndex++;
                    }

                    if (currentIndex != index)
                        return null;
                }
                else
                {
                    return null; // Invalid array index
                }
            }
            else
            {
                // Simple property navigation
                if (currentElement.ValueKind != JsonValueKind.Object)
                    return null;

                if (!currentElement.TryGetProperty(segment, out currentElement))
                    return null;
            }
        }

        return currentElement;
    }

    /// <summary>
    /// Extracts the value from a JsonElement with appropriate type conversion.
    /// Returns the value as the most appropriate .NET type (string, int, long, DateTime, bool, etc.)
    /// </summary>
    private object? ExtractValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out int intValue) ? intValue :
                                   element.TryGetInt64(out long longValue) ? longValue :
                                   element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            // For objects and arrays, return the JSON string representation
            JsonValueKind.Object => element.GetRawText(),
            JsonValueKind.Array => element.GetRawText(),
            _ => null
        };
    }
}