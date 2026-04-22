using LogSystem.Core.Services.Database;
using LogSystem.WebApp.BackgroundServices.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace LogSystem.WebApp.Endpoints.LogAttribute;

public static class DeleteLogAttributeEndpoint
{
    private const string Route = "/api/log-attributes/{id:long}";

    public static string UrlForDelete(long id) => $"/api/log-attributes/{id}";

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete(Route, async (
            [FromRoute] long? id,
            [FromServices] DatabaseService databaseService,
            [FromServices] LogAttributeCache cache) =>
        {
            var validationErrors = Validate(id);
            if (validationErrors.Count > 0)
                return Results.ValidationProblem(validationErrors);

            try
            {
                // Fetch LogAttribute by ID
                Core.Services.Database.LogAttribute? attributeToDelete = null;

                await foreach (var attr in databaseService.ListLogAttributesAsync())
                {
                    if (attr.ID == id)
                    {
                        attributeToDelete = attr;
                        break;
                    }
                }

                if (attributeToDelete == null)
                    return Results.NotFound(new { message = "LogAttribute not found." });

                // Fetch LogCollection by LogAttribute.LogCollectionID
                var logCollection = await databaseService.GetLogCollectionByIdAsync(attributeToDelete.LogCollectionID);

                if (logCollection == null)
                    return Results.NotFound(new { message = "LogCollection not found." });

                // Delete the attribute
                await databaseService.DeleteAttributeAsync(logCollection, attributeToDelete);

                // Invalidate cache
                cache.InvalidateCache(logCollection);

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
