namespace LogSystem.Core.Services.Database;

public class LogExtractionService
{
    public Log Extract(System system, IEnumerable<LogAttribute> attributes, string logContent, int sourceFileIndex, string sourceFileName)
    {
        // TODO
        // Implement method that generate a "Log" object based on a system and list of dynamic attributes
        // Each attribute should generate a dynamic record inside "Log.Attributes"
        // To extract the value of the attribute from content, check the extractionstyle of the attribute
        // Optimize to avoid unnecessary conversions, parses and memory allocation
    }
}
