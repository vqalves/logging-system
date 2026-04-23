using System.Text.Json;
using LogSystem.WebApp.Services;

namespace LogSystem.WebApp.Endpoints.LogDataEndpoints;

public static class AddLogBatchEndpoint
{
    private const string Route = "/api/log/add-batch";

    public static string UrlForAddLogBatch() => Route;

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(Route, async (
            HttpContext context,
            RabbitMqPublisher publisher,
            PublishServiceConfig config,
            string? format) =>
        {
            try
            {
                // Validate format parameter
                if (string.IsNullOrEmpty(format) || format.ToLowerInvariant() != "json")
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        { "format", new[] { "The 'format' query parameter must be 'json'. Other formats are not supported." } }
                    });
                }

                // Read the request body as bytes
                using var memoryStream = new MemoryStream();
                await context.Request.Body.CopyToAsync(memoryStream);
                var bodyBytes = memoryStream.ToArray();

                // Validate that body is not empty
                if (bodyBytes.Length == 0)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        { "Body", new[] { "Request body is required and cannot be empty." } }
                    });
                }

                // Deserialize as JSON array
                JsonElement jsonArray;
                try
                {
                    jsonArray = JsonSerializer.Deserialize<JsonElement>(bodyBytes);
                }
                catch (JsonException ex)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        { "Body", new[] { $"Request body must be valid JSON: {ex.Message}" } }
                    });
                }

                // Validate it's an array
                if (jsonArray.ValueKind != JsonValueKind.Array)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        { "Body", new[] { "Request body must be a JSON array." } }
                    });
                }

                // Validate array is not empty
                var arrayLength = jsonArray.GetArrayLength();
                if (arrayLength == 0)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        { "Body", new[] { "JSON array cannot be empty." } }
                    });
                }

                // Publish each item to RabbitMQ
                int count = 0;
                foreach (var item in jsonArray.EnumerateArray())
                {
                    // Serialize each item to bytes
                    var itemBytes = JsonSerializer.SerializeToUtf8Bytes(item);

                    // Publish to RabbitMQ
                    await publisher.PublishAsync(
                        exchange: config.RabbitMqExchangeName,
                        routingKey: config.RabbitMqRoutingKey,
                        message: itemBytes);

                    count++;
                }

                return Results.Ok(new { message = "Log messages published successfully", count });
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        });
    }
}
