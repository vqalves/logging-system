namespace LogSystem.Core.Services.Database;

public class Log
{
    public long ID { get; set; }
    public int SourceFileIndex { get; set; }
    public string SourceFileName { get; set; }
    public DateTime ValidUntilUtc { get; set; }
    
    public Dictionary<string, object> Attributes { get; set; }
}