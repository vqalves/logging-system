namespace LogSystem.Core.Services.Database;

public abstract class ExtractionStyle
{
    public static readonly ExtractionStyleJson JSON = new ExtractionStyleJson();
    public static readonly ExtractionStyleRegex REGEX = new ExtractionStyleRegex();

    public string Value { get; init; }

    protected ExtractionStyle(string value)
    {
        Value = value;
    }
}