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

                // Validate sort parameters
                var sortByValidation = ValidateSortBy(request.SortBy, allAttributes);
                if (!sortByValidation.IsValid)
                    return Results.ValidationProblem(sortByValidation.Errors!);

                string? sortBy = sortByValidation.ValidatedSortBy;

                var sortDirectionValidation = ValidateSortDirection(request.SortDirection);
                if (!sortDirectionValidation.IsValid)
                    return Results.ValidationProblem(sortDirectionValidation.Errors!);

                string? sortDirection = sortDirectionValidation.ValidatedSortDirection;

                // Validate and convert lastSortValue if provided
                var lastSortValueValidation = ValidateLastSortValue(request.LastSortValue, sortBy, allAttributes);
                if (!lastSortValueValidation.IsValid)
                    return Results.ValidationProblem(lastSortValueValidation.Errors!);

                object? lastSortValue = lastSortValueValidation.ConvertedValue;

                // Query logs with pagination
                var logs = new List<ResponseLog>();
                var limit = request.Limit ?? 100;

                await foreach (var log in databaseService.QueryLogsAsync(logCollection, allAttributes, filters, request.LastId, limit, sortBy, sortDirection, lastSortValue))
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

    private static (bool IsValid, string? ValidatedSortBy, Dictionary<string, string[]>? Errors) ValidateSortBy(string? sortBy, List<LogAttribute> allAttributes)
    {
        if (string.IsNullOrEmpty(sortBy))
        {
            return (true, null, null);
        }

        // Check if sortBy is either "ID" or a valid attribute SqlColumnName
        if (sortBy.Equals("ID", StringComparison.OrdinalIgnoreCase))
        {
            return (true, sortBy, null);
        }

        var validAttribute = allAttributes.FirstOrDefault(a =>
            a.SqlColumnName.Equals(sortBy, StringComparison.OrdinalIgnoreCase));

        if (validAttribute == null)
        {
            var errors = new Dictionary<string, string[]>
            {
                { "sortBy", new[] { $"Invalid column name '{sortBy}'. Must be 'ID' or a valid attribute column name." } }
            };
            return (false, null, errors);
        }

        // Use the exact SqlColumnName from the attribute
        return (true, validAttribute.SqlColumnName, null);
    }

    private static (bool IsValid, string? ValidatedSortDirection, Dictionary<string, string[]>? Errors) ValidateSortDirection(string? sortDirection)
    {
        if (string.IsNullOrEmpty(sortDirection))
        {
            return (true, null, null);
        }

        if (!sortDirection.Equals("ASC", StringComparison.OrdinalIgnoreCase) &&
            !sortDirection.Equals("DESC", StringComparison.OrdinalIgnoreCase))
        {
            var errors = new Dictionary<string, string[]>
            {
                { "sortDirection", new[] { "Sort direction must be either 'ASC' or 'DESC'." } }
            };
            return (false, null, errors);
        }

        return (true, sortDirection, null);
    }

    private static (bool IsValid, object? ConvertedValue, Dictionary<string, string[]>? Errors) ValidateLastSortValue(
        object? lastSortValue,
        string? sortBy,
        List<LogAttribute> allAttributes)
    {
        // If no lastSortValue provided, return valid with null
        if (lastSortValue == null)
        {
            return (true, null, null);
        }

        // If no sortBy or sorting by ID, lastSortValue is not used
        if (string.IsNullOrEmpty(sortBy) || sortBy.Equals("ID", StringComparison.OrdinalIgnoreCase))
        {
            return (true, null, null);
        }

        // Find the attribute corresponding to sortBy
        var sortAttribute = allAttributes.FirstOrDefault(a =>
            a.SqlColumnName.Equals(sortBy, StringComparison.OrdinalIgnoreCase));

        if (sortAttribute == null)
        {
            // This shouldn't happen if sortBy was validated first, but handle it gracefully
            return (true, null, null);
        }

        // Convert lastSortValue to the appropriate type based on attribute type
        try
        {
            var attributeType = sortAttribute.GetAttributeType();
            object? convertedValue = null;

            if (attributeType == AttributeType.Integer)
            {
                convertedValue = Convert.ToInt32(lastSortValue);
            }
            else if (attributeType == AttributeType.Decimal)
            {
                convertedValue = Convert.ToDecimal(lastSortValue);
            }
            else if (attributeType == AttributeType.DateTime)
            {
                convertedValue = DateTime.ParseExact(
                    lastSortValue.ToString()!,
                    "yyyy-MM-ddTHH:mm:ss",
                    null,
                    System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal
                );
            }
            else if (attributeType == AttributeType.Boolean)
            {
                convertedValue = Convert.ToBoolean(lastSortValue);
            }
            else
            {
                convertedValue = lastSortValue.ToString();
            }

            return (true, convertedValue, null);
        }
        catch (Exception)
        {
            var errors = new Dictionary<string, string[]>
            {
                { "lastSortValue", new[] { $"Invalid lastSortValue type for column '{sortBy}'." } }
            };
            return (false, null, errors);
        }
    }

    internal record Request(
        long? LastId,
        int? Limit,
        List<FilterRequest>? Filters,
        string? SortBy,
        string? SortDirection,
        object? LastSortValue
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
