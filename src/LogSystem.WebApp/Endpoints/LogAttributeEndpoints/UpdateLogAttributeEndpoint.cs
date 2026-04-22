using LogSystem.Core.Services.Database;
using LogSystem.WebApp.BackgroundServices.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace LogSystem.WebApp.Endpoints.LogAttributeEndpoints;

public static class UpdateLogAttributeEndpoint
{
    private const string Route = "/api/log-attributes/{id:long}";

    public static string UrlForUpdate(long id) => $"/api/log-attributes/{id}";

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(Route, async (
            [FromRoute] long? id,
            [FromBody] Request request,
            [FromServices] DatabaseService databaseService,
            [FromServices] LogAttributeCache cache) =>
        {
            var validationErrors = await ValidateAsync(id, request, databaseService);

            if (validationErrors.Count > 0)
                return Results.ValidationProblem(validationErrors);

            try
            {
                // Fetch existing LogAttribute by ID
                Core.Services.Database.LogAttribute? existingAttribute = null;

                await foreach (var attr in databaseService.ListLogAttributesAsync())
                {
                    if (attr.ID == id)
                    {
                        existingAttribute = attr;
                        break;
                    }
                }

                if (existingAttribute == null)
                    return Results.NotFound(new { message = "LogAttribute not found." });

                // Validate that only ExtractionExpression differs
                if (existingAttribute.LogCollectionID != request.LogCollectionID ||
                    existingAttribute.Name != request.Name ||
                    existingAttribute.SqlColumnName != request.SqlColumnName ||
                    existingAttribute.AttributeTypeID != request.AttributeTypeID ||
                    existingAttribute.ExtractionStyleID != request.ExtractionStyleID)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        { "Request", new[] { "Only ExtractionExpression can be updated. All other fields must match the existing record." } }
                    });
                }

                // Update only ExtractionExpression
                await databaseService.UpdateLogAttributeAsync(existingAttribute, request.ExtractionExpression!);

                // Fetch the LogCollection and invalidate cache
                var logCollection = await databaseService.GetLogCollectionByIdAsync(existingAttribute.LogCollectionID);
                if (logCollection != null)
                {
                    cache.InvalidateCache(logCollection);
                }

                return Results.Ok();
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        });
    }

    private static Task<Dictionary<string, string[]>> ValidateAsync(long? id, Request request, DatabaseService databaseService)
    {
        var errors = new Dictionary<string, string[]>();

        // Validate ID
        if (!id.HasValue || id <= 0)
        {
            errors.Add("id", new[] { "ID must be greater than 0." });
        }

        // Validate LogCollectionID
        if (!request.LogCollectionID.HasValue || request.LogCollectionID <= 0)
        {
            errors.Add("LogCollectionID", new[] { "LogCollectionID is required and must be greater than 0." });
        }

        // Validate Name
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add("Name", new[] { "Name is required." });
        }

        // Validate SqlColumnName
        if (string.IsNullOrWhiteSpace(request.SqlColumnName))
        {
            errors.Add("SqlColumnName", new[] { "SqlColumnName is required." });
        }

        // Validate AttributeTypeID
        if (string.IsNullOrWhiteSpace(request.AttributeTypeID))
        {
            errors.Add("AttributeTypeID", new[] { "AttributeTypeID is required." });
        }

        // Validate ExtractionStyleID
        if (string.IsNullOrWhiteSpace(request.ExtractionStyleID))
        {
            errors.Add("ExtractionStyleID", new[] { "ExtractionStyleID is required." });
        }

        // Validate ExtractionExpression
        if (string.IsNullOrWhiteSpace(request.ExtractionExpression))
        {
            errors.Add("ExtractionExpression", new[] { "ExtractionExpression is required." });
        }

        return Task.FromResult(errors);
    }

    internal record Request(
        long? LogCollectionID,
        string? Name,
        string? SqlColumnName,
        string? AttributeTypeID,
        string? ExtractionStyleID,
        string? ExtractionExpression
    );
}
