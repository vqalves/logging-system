namespace LogSystem.Core.Services.Database;

public class LogAttribute
{
    public long ID { get; set; }
    public long SystemID { get; set; }
    public string Name { get; set; }
    public string SqlColumnName { get; set; }
    public string AttributeTypeID { get; set; }
    public string ExtractionStyleID { get; set; }

    /// <summary>
    /// Expression related ot the style of expression
    /// For example, "$.person.name" for ExtractionStyle=JSON
    /// </summary>
    public string ExtractionExpression { get; set; }

    public AttributeType GetAttributeType() => AttributeType.Parse(AttributeTypeID)!;
    public bool IsAttributeType(AttributeType attributeType) => attributeType.Value.Equals(AttributeTypeID, StringComparison.InvariantCultureIgnoreCase);
}