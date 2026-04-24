using LogSystem.Core.Services.Database;
using LogSystem.Core.Caching;
using Microsoft.AspNetCore.Mvc;

namespace LogSystem.WebApp.Endpoints.LogAttributeEndpoints;

public static class CreateLogAttributeEndpoint
{
    private const string Route = "/api/log-attributes";

    public static string UrlForCreate() => Route;

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(Route, async (
            [FromBody] Request request,
            [FromServices] DatabaseService databaseService,
            [FromServices] LogAttributeCache cache) =>
        {
            var validationErrors = await ValidateAsync(request, databaseService);

            if (validationErrors.Count > 0)
                return Results.ValidationProblem(validationErrors);

            try
            {
                // Fetch the LogCollection
                var logCollection = await databaseService.GetLogCollectionByIdAsync(request.LogCollectionID);

                if (logCollection == null)
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        { "LogCollectionID", new[] { "LogCollection not found." } }
                    });

                // Create the LogAttribute
                var logAttribute = new Core.Services.Database.LogAttribute(
                    request.LogCollectionID!.Value,
                    request.Name!,
                    request.SqlColumnName!,
                    request.AttributeTypeID!,
                    request.ExtractionStyleID!,
                    request.ExtractionExpression!
                );

                await databaseService.CreateAttributeAsync(logCollection, logAttribute);

                // Invalidate cache
                cache.InvalidateCache(logCollection);

                return Results.Ok();
            }
            catch (ArgumentException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    { "SqlColumnName", new[] { ex.Message } }
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        });
    }

    private static Task<Dictionary<string, string[]>> ValidateAsync(Request request, DatabaseService databaseService)
    {
        var errors = new Dictionary<string, string[]>();

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
        else if (!System.Text.RegularExpressions.Regex.IsMatch(request.SqlColumnName, @"^[a-zA-Z0-9_]+$"))
        {
            errors.Add("SqlColumnName", new[] { "SqlColumnName must contain only alphanumeric characters and underscores." });
        }

        // Validate AttributeTypeID
        if (string.IsNullOrWhiteSpace(request.AttributeTypeID))
        {
            errors.Add("AttributeTypeID", new[] { "AttributeTypeID is required." });
        }
        else
        {
            var attributeType = AttributeType.Parse(request.AttributeTypeID);
            if (attributeType == null)
            {
                errors.Add("AttributeTypeID", new[] { "Invalid AttributeTypeID. Valid values are: text, integer, datetime, boolean, decimal." });
            }
        }

        // Validate ExtractionStyleID
        if (string.IsNullOrWhiteSpace(request.ExtractionStyleID))
        {
            errors.Add("ExtractionStyleID", new[] { "ExtractionStyleID is required." });
        }
        else if (!request.ExtractionStyleID.Equals("json", StringComparison.InvariantCultureIgnoreCase))
        {
            errors.Add("ExtractionStyleID", new[] { "Invalid ExtractionStyleID. Currently only 'json' is supported." });
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
