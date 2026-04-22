using LogSystem.WebApp.Services;

namespace LogSystem.WebApp.Endpoints.LogData;

public static class AddLogEndpoint
{
    private const string Route = "/api/log/add";

    public static string UrlForAddLog() => Route;

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(Route, async (
            HttpContext context,
            RabbitMqPublisher publisher,
            PublishServiceConfig config) =>
        {
            try
            {
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

                // Publish to RabbitMQ
                await publisher.PublishAsync(
                    exchange: config.RabbitMqExchangeName,
                    routingKey: config.RabbitMqRoutingKey,
                    message: bodyBytes);

                return Results.Ok(new { message = "Log message published successfully" });
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        });
    }
}
