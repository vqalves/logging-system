namespace LogSystem.Core.Services.Database;

public record AttributeType
{
    public static readonly AttributeType Text = new AttributeType("text", sqlDataType: "VARCHAR(8000)");
    public static readonly AttributeType Integer = new AttributeType("integer", sqlDataType: "INT");
    public static readonly AttributeType DateTime = new AttributeType("datetime", "DATETIME2");
    public static readonly AttributeType Boolean = new AttributeType("boolean", sqlDataType: "BIT");
    public static readonly AttributeType Decimal = new AttributeType("decimal", sqlDataType: "DECIMAL(18,4)");

    public string Value { get; init; }

    public string SqlDataType { get; init; }

    private AttributeType(string value, string sqlDataType)
    {
        Value = value;
        SqlDataType = sqlDataType;
    }

    public static IEnumerable<AttributeType> All()
    {
        yield return Text;
        yield return Integer;
        yield return DateTime;
        yield return Boolean;
        yield return Decimal;
    }

    public static AttributeType? Parse(string? text)
    {
        if(string.IsNullOrWhiteSpace(text)) return null;
        return All().FirstOrDefault(x => x.Value.Equals(text, StringComparison.InvariantCultureIgnoreCase));
    }

    public static bool TryParse(string? text, out AttributeType? attributeType)
    {
        attributeType = Parse(text);
        return attributeType != null;
    }
}
