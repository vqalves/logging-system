using LogSystem.Core.Services.Azure;
using LogSystem.Core.Services.Database;
using LogSystem.WebApp.BackgroundServices.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace LogSystem.WebApp.Endpoints.LogCollectionEndpoints;

public static class CreateOrUpdateLogCollectionEndpoint
{
    private const string Route = "/api/log-collections";

    public static string UrlForCreateOrUpdate() => Route;

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(Route, async (
            [FromBody] Request request,
            [FromServices] DatabaseService databaseService,
            [FromServices] LogCollectionCache cache,
            [FromServices] AzureService azureService) =>
        {
            var validationErrors = await ValidateAsync(request, databaseService);

            if (validationErrors.Count > 0)
                return Results.ValidationProblem(validationErrors);

            try
            {
                Core.Services.Database.LogCollection logCollection;
                bool isNewCollection = false;
                bool retentionChanged = false;

                if (request.ID == null || request.ID == 0)
                {
                    // Create new collection
                    logCollection = new LogCollection(request.Name!, request.ClientId!, request.TableName!, request.LogDurationHours!.Value);
                    isNewCollection = true;
                }
                else
                {
                    // Update existing collection - fetch it first
                    var existing = await databaseService.GetLogCollectionByIdAsync(request.ID);

                    if (existing == null)
                        return Results.NotFound(new { message = "LogCollection not found." });

                    retentionChanged = existing.LogDurationHours != request.LogDurationHours!.Value;

                    logCollection = existing;
                    logCollection.Name = request.Name!;
                    logCollection.ClientId = request.ClientId!;
                    logCollection.LogDurationHours = request.LogDurationHours!.Value;
                }

                await databaseService.SaveLogCollectionAsync(logCollection);

                // Create or update Azure lifecycle policy
                if (isNewCollection)
                {
                    await azureService.CreateLifecyclePolicyAsync(logCollection.TableName, logCollection.LogDurationHours);
                }
                else if (retentionChanged)
                {
                    await azureService.UpdateLifecyclePolicyAsync(logCollection.TableName, logCollection.LogDurationHours);
                }

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

    private static async Task<Dictionary<string, string[]>> ValidateAsync(Request request, DatabaseService databaseService)
    {
        var errors = new Dictionary<string, string[]>();

        // Validate Name
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add("Name", new[] { "Name is required." });
        }

        // Validate ClientId using static validation method
        if (!Core.Services.Database.LogCollection.TryValidateClientId(request.ClientId ?? "", out var clientIdError))
        {
            errors.Add("ClientId", new[] { clientIdError! });
        }
        else
        {
            // Check if ClientId is already used by another record
            var existingByClientId = await databaseService.GetLogCollectionByClientIdAsync(request.ClientId!);

            if (existingByClientId != null && existingByClientId.ID != request.ID)
                errors.Add("ClientId", new[] { "This ClientId is already in use by another LogCollection." });
        }

        // Validate TableName using static validation method
        if (!Core.Services.Database.LogCollection.TryValidateTableName(request.TableName ?? "", out var tableNameError))
        {
            errors.Add("TableName", new[] { tableNameError! });
        }
        else
        {
            // Check if TableName is already used by another record
            var existingByTableName = await databaseService.ListLogCollectionsAsync(tableNameEquals: request.TableName!).FirstOrDefaultAsync();
            
            if (existingByTableName != null && existingByTableName.ID != request.ID)
                errors.Add("TableName", new[] { "This TableName is already in use by another LogCollection." });
        }

        // Validate LogDurationHours
        if (!request.LogDurationHours.HasValue || request.LogDurationHours <= 0)
        {
            errors.Add("LogDurationHours", new[] { "LogDurationHours must be greater than 0." });
        }

        return errors;
    }

    internal record Request(long? ID, string? Name, string? ClientId, string? TableName, long? LogDurationHours);
}
