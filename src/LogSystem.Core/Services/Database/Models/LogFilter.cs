namespace LogSystem.Core.Services.Database;

/// <summary>
/// Represents a filter condition for querying logs based on dynamic attributes.
/// </summary>
/// <example>
/// <code>
/// // Age >= 18
/// new LogFilter { LogAttributeID = 1, Operator = FilterOperator.GreaterThanOrEqual, Value = "18" }
///
/// // Name STARTS WITH 'abc'
/// new LogFilter { LogAttributeID = 2, Operator = FilterOperator.StartsWith, Value = "abc" }
///
/// // Description CONTAINS 'xyz'
/// new LogFilter { LogAttributeID = 3, Operator = FilterOperator.Contains, Value = "xyz" }
///
/// // Date < '2026-01-01'
/// new LogFilter { LogAttributeID = 4, Operator = FilterOperator.LessThan, Value = "2026-01-01" }
///
/// // Status IS NULL
/// new LogFilter { LogAttributeID = 5, Operator = FilterOperator.IsNull, Value = null }
/// </code>
/// </example>
public class LogFilter
{
    /// <summary>
    /// The ID of the LogAttribute to filter on.
    /// References LogAttribute.ID which defines the column name and data type.
    /// </summary>
    public long LogAttributeID { get; set; }

    /// <summary>
    /// The comparison operator to apply.
    /// Use FilterOperator constants (e.g., FilterOperator.Equal, FilterOperator.Contains).
    /// </summary>
    public string Operator { get; set; } = null!;

    /// <summary>
    /// The value to compare against, stored as a string.
    /// Will be converted to the appropriate type based on the LogAttribute's AttributeTypeID when building SQL queries.
    /// For IsNull/IsNotNull operators, this value is ignored.
    /// </summary>
    public object? Value { get; set; }
}

/// <summary>
/// SmartEnum representing filter operators for LogFilter conditions.
/// </summary>
public record FilterOperator
{
    // Comparison operators
    public static readonly FilterOperator Equal = new FilterOperator("=", "Equal");
    public static readonly FilterOperator NotEqual = new FilterOperator("!=", "NotEqual");
    public static readonly FilterOperator LessThan = new FilterOperator("<", "LessThan");
    public static readonly FilterOperator LessThanOrEqual = new FilterOperator("<=", "LessThanOrEqual");
    public static readonly FilterOperator GreaterThan = new FilterOperator(">", "GreaterThan");
    public static readonly FilterOperator GreaterThanOrEqual = new FilterOperator(">=", "GreaterThanOrEqual");

    // String operators
    public static readonly FilterOperator Contains = new FilterOperator("CONTAINS", "Contains");
    public static readonly FilterOperator StartsWith = new FilterOperator("STARTSWITH", "StartsWith");
    public static readonly FilterOperator EndsWith = new FilterOperator("ENDSWITH", "EndsWith");

    // Null operators
    public static readonly FilterOperator IsNull = new FilterOperator("ISNULL", "IsNull");
    public static readonly FilterOperator IsNotNull = new FilterOperator("ISNOTNULL", "IsNotNull");

    /// <summary>
    /// The string value used for serialization/deserialization (e.g., "=", "CONTAINS").
    /// </summary>
    public string Value { get; init; }

    /// <summary>
    /// The friendly name of the operator (e.g., "Equal", "Contains").
    /// </summary>
    public string Name { get; init; }

    private FilterOperator(string value, string name)
    {
        Value = value;
        Name = name;
    }

    /// <summary>
    /// Returns all available filter operators.
    /// </summary>
    public static IEnumerable<FilterOperator> All()
    {
        yield return Equal;
        yield return NotEqual;
        yield return LessThan;
        yield return LessThanOrEqual;
        yield return GreaterThan;
        yield return GreaterThanOrEqual;
        yield return Contains;
        yield return StartsWith;
        yield return EndsWith;
        yield return IsNull;
        yield return IsNotNull;
    }

    /// <summary>
    /// Parses a string value to a FilterOperator.
    /// </summary>
    /// <param name="text">The operator string (e.g., "=", "CONTAINS", ">=").</param>
    /// <returns>The matching FilterOperator or null if not found.</returns>
    public static FilterOperator? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        return All().FirstOrDefault(x => x.Value.Equals(text, StringComparison.InvariantCultureIgnoreCase));
    }

    /// <summary>
    /// Tries to parse a string value to a FilterOperator.
    /// </summary>
    /// <param name="text">The operator string.</param>
    /// <param name="filterOperator">The parsed FilterOperator if successful.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>
    public static bool TryParse(string? text, out FilterOperator? filterOperator)
    {
        filterOperator = Parse(text);
        return filterOperator != null;
    }
}
