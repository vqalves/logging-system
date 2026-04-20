using System.Text.RegularExpressions;

namespace LogSystem.Core.Services.Database;

public class LogAttribute
{
    public long ID { get; set; }
    public long LogCollectionID { get; }
    public string Name { get; }
    public string SqlColumnName { get; }
    public string AttributeTypeID { get; }
    public string ExtractionStyleID { get; }

    /// <summary>
    /// Expression related ot the style of expression
    /// For example, "$.person.name" for ExtractionStyle=JSON
    /// </summary>
    public string ExtractionExpression { get; }

    public LogAttribute(long logCollectionID, string name, string sqlColumnName, string attributeTypeID, string extractionStyleID, string extractionExpression)
    {
        // Validate SqlColumnName to prevent SQL injection (alphanumeric + underscore only)
        if (!Regex.IsMatch(sqlColumnName, @"^[a-zA-Z0-9_]+$"))
        {
            throw new ArgumentException("SqlColumnName must contain only alphanumeric characters and underscores.", nameof(sqlColumnName));
        }

        LogCollectionID = logCollectionID;
        Name = name;
        SqlColumnName = sqlColumnName;
        AttributeTypeID = attributeTypeID;
        ExtractionStyleID = extractionStyleID;
        ExtractionExpression = extractionExpression;
    }

    public bool HasExtractionStyle(ExtractionStyle extractionStyle) => extractionStyle.Value.Equals(ExtractionStyleID, StringComparison.InvariantCultureIgnoreCase);

    public AttributeType GetAttributeType() => AttributeType.Parse(AttributeTypeID)!;
    public bool IsAttributeType(AttributeType attributeType) => attributeType.Value.Equals(AttributeTypeID, StringComparison.InvariantCultureIgnoreCase);
}