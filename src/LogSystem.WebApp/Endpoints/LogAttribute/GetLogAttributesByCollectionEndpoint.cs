using LogSystem.Core.Services.Database;
using Microsoft.AspNetCore.Mvc;

namespace LogSystem.WebApp.Endpoints.LogAttribute;

public static class GetLogAttributesByCollectionEndpoint
{
    private const string Route = "/api/log-collections/{logCollectionId:long}/log-attributes";

    public static string UrlForGetByCollection(long logCollectionId) => $"/api/log-collections/{logCollectionId}/log-attributes";

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(Route, async (
            [FromRoute] long? logCollectionId,
            [FromServices] DatabaseService databaseService) =>
        {
            var validationErrors = Validate(logCollectionId);
            if (validationErrors.Count > 0)
                return Results.ValidationProblem(validationErrors);

            try
            {
                // Validate LogCollection exists
                var logCollection = await databaseService.GetLogCollectionByIdAsync(logCollectionId);

                if (logCollection == null)
                    return Results.NotFound(new { message = "LogCollection not found." });

                // Fetch all attributes for the collection
                var attributes = new Response();

                await foreach (var attribute in databaseService.ListAttributesOfCollectionAsync(logCollection))
                {
                    attributes.Add(new ResponseItem(
                        attribute.ID,
                        attribute.LogCollectionID,
                        attribute.Name,
                        attribute.SqlColumnName,
                        attribute.AttributeTypeID,
                        attribute.ExtractionStyleID,
                        attribute.ExtractionExpression
                    ));
                }

                return Results.Ok(attributes);
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        });
    }

    private static Dictionary<string, string[]> Validate(long? logCollectionId)
    {
        var errors = new Dictionary<string, string[]>();

        if (!logCollectionId.HasValue || logCollectionId <= 0)
        {
            errors.Add("logCollectionId", new[] { "LogCollectionId must be greater than 0." });
        }

        return errors;
    }

    internal class Response() : List<ResponseItem>;
    internal record ResponseItem(
        long ID,
        long LogCollectionID,
        string Name,
        string SqlColumnName,
        string AttributeTypeID,
        string ExtractionStyleID,
        string ExtractionExpression
    );
}
