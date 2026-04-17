namespace LogSystem.Core.Services.Database;

public abstract class ExtractionStyle
{
    public static readonly ExtractionStyleJson JSON = new ExtractionStyleJson();
    
    public string Value { get; init; }

    protected ExtractionStyle(string value)
    {
        Value = value;
    }
}