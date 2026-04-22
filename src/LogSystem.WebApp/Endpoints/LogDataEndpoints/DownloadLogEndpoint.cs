using LogSystem.Core.Services.Azure;
using LogSystem.Core.Services.Database;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace LogSystem.WebApp.Endpoints.LogDataEndpoints;

public static class DownloadLogEndpoint
{
    private const string Route = "/api/log-collections/{logCollectionId:long}/logs/{logId:long}/download";

    public static string UrlForDownload(long logCollectionId, long logId) => $"/api/log-collections/{logCollectionId}/logs/{logId}/download";

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(Route, async (
            [FromRoute] long? logCollectionId,
            [FromRoute] long? logId,
            [FromServices] DatabaseService databaseService,
            [FromServices] LogDataService logDataService,
            [FromServices] AzureService azureService) =>
        {
            var validationErrors = Validate(logCollectionId, logId);
            if (validationErrors.Count > 0)
                return Results.ValidationProblem(validationErrors);

            try
            {
                // Validate LogCollection exists
                var logCollection = await databaseService.GetLogCollectionByIdAsync(logCollectionId);
                if (logCollection == null)
                    return Results.NotFound(new { message = "LogCollection not found." });

                // Query for the specific log
                Log? targetLog = await logDataService.QueryLogAsync(logCollection, logId!.Value);
                if (targetLog == null)
                    return Results.NotFound(new { message = "Log not found." });

                // Download the file from Azure
                var downloadedFile = await azureService.DownloadFileAsync(
                    collectionName: logCollection.TableName,
                    fileName: targetLog.SourceFileName);

                if (!downloadedFile.Found || downloadedFile.Content == null)
                    return Results.NotFound(new { message = "Log file not found in Azure storage." });

                // Parse the JSON array
                string[]? logArray;
                try
                {
                    logArray = JsonSerializer.Deserialize<string[]>(downloadedFile.Content);
                }
                catch (JsonException)
                {
                    return Results.Problem(detail: "Failed to parse log file content.", statusCode: 500);
                }

                if (logArray == null || targetLog.SourceFileIndex >= logArray.Length)
                    return Results.Problem(detail: "Log index out of range in file.", statusCode: 500);

                // Get the raw log content at the specified index
                var rawLogContent = logArray[targetLog.SourceFileIndex];

                // Return as text/plain with download filename
                var fileName = $"{logCollection.ClientId}_{logId}.txt";

                // Convert string to byte stream
                var contentBytes = Encoding.UTF8.GetBytes(rawLogContent);
                var memoryStream = new MemoryStream(contentBytes);

                return Results.Stream(
                    stream: memoryStream,
                    contentType: "text/plain",
                    fileDownloadName: fileName);
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        });
    }

    private static Dictionary<string, string[]> Validate(long? logCollectionId, long? logId)
    {
        var errors = new Dictionary<string, string[]>();

        if (!logCollectionId.HasValue || logCollectionId <= 0)
        {
            errors.Add("logCollectionId", new[] { "LogCollectionId must be greater than 0." });
        }

        if (!logId.HasValue || logId <= 0)
        {
            errors.Add("logId", new[] { "LogId must be greater than 0." });
        }

        return errors;
    }
}
