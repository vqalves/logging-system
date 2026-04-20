using LogSystem.Core.Services.Database;
using LogSystem.WebApp.BackgroundServices.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace LogSystem.WebApp.Endpoints;

public static class CreateOrUpdateLogCollectionEndpoint
{
    private const string Route = "/api/log-collections";

    public static string UrlForCreateOrUpdate() => Route;

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(Route, async (
            [FromBody] Request request,
            [FromServices] DatabaseService databaseService,
            [FromServices] LogCollectionCache cache) =>
        {
            var validationErrors = Validate(request);

            if (validationErrors.Count > 0)
                return Results.ValidationProblem(validationErrors);

            try
            {
                // TODO: Move logic of ValidateUniqueness to Validate method
                // Check for uniqueness of ClientId and TableName
                var uniquenessErrors = await ValidateUniqueness(request, databaseService);
                if (uniquenessErrors.Count > 0)
                    return Results.ValidationProblem(uniquenessErrors);

                LogCollection logCollection;

                if (request.ID == null || request.ID == 0)
                {
                    // Create new collection
                    logCollection = new LogCollection(request.Name!, request.ClientId!, request.TableName!, request.LogDurationHours!.Value);
                }
                else
                {
                    // Update existing collection - fetch it first
                    var existing = await databaseService.GetLogCollectionByIdAsync(request.ID);

                    if (existing == null)
                        return Results.NotFound(new { message = "LogCollection not found." });

                    logCollection = existing;
                    logCollection.Name = request.Name!;
                    logCollection.ClientId = request.ClientId!;
                    logCollection.LogDurationHours = request.LogDurationHours!.Value;
                }

                await databaseService.SaveLogCollectionAsync(logCollection);

                // Invalidate cache
                cache.InvalidateCache(logCollection);

                return Results.Ok();
            }
            catch (ArgumentException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    { "ClientId", new[] { ex.Message } }
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        });
    }

    private static Dictionary<string, string[]> Validate(Request request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add("Name", new[] { "Name is required." });
        }

        if (string.IsNullOrWhiteSpace(request.ClientId))
        {
            errors.Add("ClientId", new[] { "ClientId is required." });
        }
        else if (!System.Text.RegularExpressions.Regex.IsMatch(request.ClientId, @"^[a-zA-Z0-9_.-]+$"))
        {
            errors.Add("ClientId", new[] { "ClientId must contain only alphanumeric characters, hyphens, underscores, and dots." });
        }

        if (string.IsNullOrWhiteSpace(request.TableName))
        {
            errors.Add("TableName", new[] { "TableName is required." });
        }
        else if (!System.Text.RegularExpressions.Regex.IsMatch(request.TableName, @"^[a-zA-Z0-9_]+$"))
        {
            errors.Add("TableName", new[] { "TableName must contain only alphanumeric characters and underscores." });
        }

        if (!request.LogDurationHours.HasValue || request.LogDurationHours <= 0)
        {
            errors.Add("LogDurationHours", new[] { "LogDurationHours must be greater than 0." });
        }

        return errors;
    }

    private static async Task<Dictionary<string, string[]>> ValidateUniqueness(Request request, DatabaseService databaseService)
    {
        var errors = new Dictionary<string, string[]>();

        // Check if ClientId is already used by another record
        var existingByClientId = await databaseService.GetLogCollectionByClientIdAsync(request.ClientId!);
        if (existingByClientId != null && existingByClientId.ID != request.ID)
        {
            errors.Add("ClientId", new[] { "This ClientId is already in use by another LogCollection." });
        }

        // Check if TableName is already used by another record
        var existingByTableName = await databaseService.ListLogCollectionsAsync(tableNameEquals: request.TableName!).FirstOrDefaultAsync();
        if (existingByTableName != null && existingByTableName.ID != request.ID)
        {
            errors.Add("TableName", new[] { "This TableName is already in use by another LogCollection." });
        }

        return errors;
    }

    internal record Request(long? ID, string? Name, string? ClientId, string? TableName, long? LogDurationHours);
}
