using LogSystem.Core.Services.Database;
using LogSystem.WebApp.BackgroundServices.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace LogSystem.WebApp.Endpoints.LogCollection;

public static class DeleteLogCollectionEndpoint
{
    private const string Route = "/api/log-collections/{id:long}";

    public static string UrlForDelete(long id) => $"/api/log-collections/{id}";

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete(Route, async (
            [FromRoute] long? id,
            [FromServices] DatabaseService databaseService,
            [FromServices] LogCollectionCache cache) =>
        {
            var validationErrors = Validate(id);
            if (validationErrors.Count > 0)
                return Results.ValidationProblem(validationErrors);

            try
            {
                // Find the collection by iterating through all collections
                Core.Services.Database.LogCollection? collectionToDelete = await databaseService.GetLogCollectionByIdAsync(id);

                if (collectionToDelete == null)
                    return Results.NotFound(new { message = "LogCollection not found." });

                await databaseService.DeleteLogCollectionAsync(collectionToDelete);

                // Invalidate cache
                cache.InvalidateCache(collectionToDelete);

                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        });
    }

    private static Dictionary<string, string[]> Validate(long? id)
    {
        var errors = new Dictionary<string, string[]>();

        if (!id.HasValue || id <= 0)
        {
            errors.Add("id", new[] { "ID must be greater than 0." });
        }

        return errors;
    }
}
