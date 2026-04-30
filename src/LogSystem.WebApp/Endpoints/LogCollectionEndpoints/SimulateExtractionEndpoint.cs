using System.Text;
using System.Text.Json;
using LogSystem.Core.Services.Database;
using Microsoft.AspNetCore.Mvc;

namespace LogSystem.WebApp.Endpoints.LogCollectionEndpoints;

public static class SimulateExtractionEndpoint
{
    private const string Route = "/api/log-collections/{logCollectionId:long}/simulate";

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(Route, async (
            [FromRoute] long? logCollectionId,
            [FromBody] Request? request,
            [FromServices] DatabaseService databaseService,
            [FromServices] LogExtractionService logExtractionService) =>
        {
            var validationErrors = Validate(logCollectionId, request);
            if (validationErrors.Count > 0)
                return Results.ValidationProblem(validationErrors);

            try
            {
                // Validate LogCollection exists
                var logCollection = await databaseService.GetLogCollectionByIdAsync(logCollectionId);

                if (logCollection == null)
                    return Results.NotFound(new { message = "LogCollection not found." });

                // Get all attributes for the collection
                var attributes = new List<LogAttribute>();
                await foreach (var attribute in databaseService.ListAttributesOfCollectionAsync(logCollection))
                {
                    attributes.Add(attribute);
                }

                if (attributes.Count == 0)
                {
                    return Results.Ok(new Response { Results = new List<ResponseItem>() });
                }

                // Extract attributes from the log text
                var extractedLog = logExtractionService.Extract(
                    logCollection,
                    attributes,
                    contentAsJsonDocument: () =>
                    {
                        try
                        {
                            return JsonDocument.Parse(request!.LogText!);
                        }
                        catch
                        {
                            return null;
                        }
                    },
                    contentAsString: () => request!.LogText!
                );

                // Build response with attribute details and extracted values
                var results = attributes.Select(attr => new ResponseItem(
                    AttributeName: attr.Name,
                    SqlColumnName: attr.SqlColumnName,
                    AttributeType: attr.AttributeTypeID,
                    ExtractionStyle: attr.ExtractionStyleID,
                    ExtractionExpression: attr.ExtractionExpression,
                    ExtractedValue: extractedLog.Attributes.TryGetValue(attr.SqlColumnName, out var value) ? value : null
                )).ToList();

                return Results.Ok(new Response { Results = results });
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        });
    }

    private static Dictionary<string, string[]> Validate(long? logCollectionId, Request? request)
    {
        var errors = new Dictionary<string, string[]>();

        if (!logCollectionId.HasValue || logCollectionId <= 0)
        {
            errors.Add("logCollectionId", new[] { "LogCollectionId must be greater than 0." });
        }

        if (request == null)
        {
            errors.Add("request", new[] { "Request body is required." });
            return errors;
        }

        if (string.IsNullOrWhiteSpace(request.LogText))
        {
            errors.Add(nameof(request.LogText), new[] { "LogText is required." });
        }

        return errors;
    }

    internal record Request(string? LogText);

    internal class Response
    {
        public List<ResponseItem> Results { get; set; } = new();
    }

    internal record ResponseItem(
        string AttributeName,
        string SqlColumnName,
        string AttributeType,
        string ExtractionStyle,
        string ExtractionExpression,
        object? ExtractedValue
    );
}
