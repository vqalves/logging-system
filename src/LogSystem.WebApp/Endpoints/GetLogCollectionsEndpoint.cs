using LogSystem.Core.Services.Database;
using Microsoft.AspNetCore.Mvc;

namespace LogSystem.WebApp.Endpoints;

public static class GetLogCollectionsEndpoint
{
    private const string Route = "/api/log-collections";

    public static string UrlForGetLogCollections() => Route;

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(Route, async ([FromServices] DatabaseService databaseService) =>
        {
            var collections = new Response();
            
            await foreach (var collection in databaseService.ListLogCollectionsAsync())
            {
                collections.Add(new ResponseItem(
                    collection.ID,
                    collection.Name,
                    collection.ClientId,
                    collection.TableName,
                    collection.LogDurationHours
                ));
            }
            return Results.Ok(collections);
        });
    }

    internal class Response() : List<ResponseItem>;
    internal record ResponseItem(long ID, string Name, string ClientId, string TableName, long LogDurationHours);
}

