using LogSystem.Core.Services.Database;
using Microsoft.AspNetCore.Mvc;

namespace LogSystem.WebApp.Endpoints.LogDataEndpoints;

public static class SearchLogsEndpoint
{
    private const string Route = "/api/log-collections/{logCollectionId:long}/logs/search";

    public static string UrlForSearch(long logCollectionId) => $"/api/log-collections/{logCollectionId}/logs/search";

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(Route, async (
            [FromRoute] long? logCollectionId,
            [FromBody] Request? request,
            [FromServices] DatabaseService databaseService) =>
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

                // Get all attributes for this collection
                var allAttributes = new List<LogAttribute>();
                await foreach (var attribute in databaseService.ListAttributesOfCollectionAsync(logCollection))
                {
                    allAttributes.Add(attribute);
                }

                // Validate filters reference valid attributes
                var filters = new List<LogFilter>();
                if (request!.Filters != null)
                {
                    foreach (var filterRequest in request.Filters)
                    {
                        var attribute = allAttributes.FirstOrDefault(a => a.ID == filterRequest.LogAttributeID);
                        if (attribute == null)
                        {
                            return Results.ValidationProblem(new Dictionary<string, string[]>
                            {
                                { "filters", new[] { $"LogAttributeID {filterRequest.LogAttributeID} does not exist for this collection." } }
                            });
                        }

                        var filterOperator = FilterOperator.Parse(filterRequest.Operator);
                        if (filterOperator == null)
                        {
                            return Results.ValidationProblem(new Dictionary<string, string[]>
                            {
                                { "filters", new[] { $"Invalid operator '{filterRequest.Operator}'. Valid operators: {string.Join(", ", FilterOperator.All().Select(o => o.Value))}" } }
                            });
                        }

                        filters.Add(new LogFilter
                        {
                            LogAttributeID = filterRequest.LogAttributeID!.Value,
                            Operator = filterRequest.Operator!,
                            Value = filterRequest.Value
                        });
                    }
                }

                // Query logs with pagination
                var logs = new List<ResponseLog>();
                var limit = request.Limit ?? 100;

                await foreach (var log in databaseService.QueryLogsAsync(logCollection, allAttributes, filters, request.LastId, limit))
                {
                    var responseLog = new ResponseLog(
                        log.ID,
                        log.SourceFileIndex,
                        log.SourceFileName,
                        log.ValidUntilUtc,
                        new Dictionary<string, object?>()
                    );

                    // Add dynamic attributes
                    foreach (var attribute in allAttributes)
                    {
                        if (log.Attributes.TryGetValue(attribute.SqlColumnName, out var value))
                        {
                            responseLog.Attributes[attribute.SqlColumnName] = value;
                        }
                        else
                        {
                            responseLog.Attributes[attribute.SqlColumnName] = null;
                        }
                    }

                    logs.Add(responseLog);
                }

                var response = new Response(
                    logs,
                    logs.Count == limit ? logs.LastOrDefault()?.ID : null,
                    allAttributes.Select(a => new AttributeInfo(
                        a.ID,
                        a.Name,
                        a.SqlColumnName,
                        a.AttributeTypeID
                    )).ToList()
                );

                return Results.Ok(response);
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

        if (request.LastId.HasValue && request.LastId <= 0)
        {
            errors.Add("lastId", new[] { "LastId must be greater than 0." });
        }

        if (request.Limit.HasValue && (request.Limit <= 0 || request.Limit > 1000))
        {
            errors.Add("limit", new[] { "Limit must be between 1 and 1000." });
        }

        return errors;
    }

    internal record Request(
        long? LastId,
        int? Limit,
        List<FilterRequest>? Filters
    );

    internal record FilterRequest(
        long? LogAttributeID,
        string? Operator,
        object? Value
    );

    internal record Response(
        List<ResponseLog> Logs,
        long? NextLastId,
        List<AttributeInfo> Attributes
    );

    internal record ResponseLog(
        long ID,
        int SourceFileIndex,
        string SourceFileName,
        DateTime ValidUntilUtc,
        Dictionary<string, object?> Attributes
    );

    internal record AttributeInfo(
        long ID,
        string Name,
        string SqlColumnName,
        string AttributeTypeID
    );
}
