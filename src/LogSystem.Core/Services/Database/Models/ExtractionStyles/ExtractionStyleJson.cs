namespace LogSystem.Core.Services.Database;

public class ExtractionStyleJson : ExtractionStyle
{    
    public ExtractionStyleJson() : base("json")
    {
        
    }

    // TODO: Implement method to extract a value from a string based on an expression
    // For example, "$.person.name" from '{ "person": { "name": "João" }}' should return "João" 
    // Define the best type for "content" - it can be string, or JsonNode, JsonDocument or similar
    public object Extract(object? content, string expression)
    {
        
    }
}
