using LogSystem.Core.Metrics;

namespace LogSystem.WebApp.Endpoints.LogCollectionEndpoints;

public static class GetCollectionMetricsEndpoint
{
    private const string Route = "/api/log-collections/metrics";

    public static string UrlForGetMetrics() => Route;

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(Route, (
            MessagesPerCollectionInTimeWindowReport metricsReport) =>
        {
            var stats = metricsReport.GetCurrentStats();

            var response = stats.Select(kvp => new CollectionMetric
            {
                CollectionClientId = kvp.Key,
                SuccessCount = kvp.Value.SuccessCount,
                FailedCount = kvp.Value.FailedCount,
                TotalCount = kvp.Value.TotalCount,
                AverageMessagesPerSecond = CalculateAverageMessagesPerSecond(kvp.Value)
            }).ToList();

            return Results.Ok(response);
        });
    }

    private static double CalculateAverageMessagesPerSecond(MessagesPerCollectionInTimeWindowReport.CollectionStats stats)
    {
        double retentionWindowSeconds = MessagesPerCollectionInTimeWindowReport.RetentionWindow.TotalSeconds;

        return stats.TotalCount / retentionWindowSeconds;
    }

    internal record CollectionMetric
    {
        public required string CollectionClientId { get; init; }
        public required int SuccessCount { get; init; }
        public required int FailedCount { get; init; }
        public required int TotalCount { get; init; }
        public required double AverageMessagesPerSecond { get; init; }
    }
}
