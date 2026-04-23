using Microsoft.Extensions.Logging;

namespace LogSystem.Core.Metrics;

public class MessageReceivedReport(ILogger logger)
{
    private readonly object _lock = new();
    private readonly Dictionary<string, ChannelMetrics> _channelMetrics = [];

    private class ChannelMetrics
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public TimeSpan TotalProcessingTime { get; set; }
    }

    public void RecordMessage(string channelId, TimeSpan processingTime, bool success)
    {
        lock (_lock)
        {
            if (!_channelMetrics.ContainsKey(channelId))
                _channelMetrics[channelId] = new ChannelMetrics();

            var metrics = _channelMetrics[channelId];

            if (success)
                metrics.SuccessCount++;
            else
                metrics.FailureCount++;

            metrics.TotalProcessingTime += processingTime;
        }
    }

    public void WriteReportIfNeeded()
    {
        Dictionary<string, ChannelMetrics> snapshot;

        lock (_lock)
        {
            // Check if there are any messages to report
            if (_channelMetrics.Count == 0 || _channelMetrics.All(x => x.Value.SuccessCount == 0 && x.Value.FailureCount == 0))
                return;

            // Create snapshot and reset metrics
            snapshot = new Dictionary<string, ChannelMetrics>(_channelMetrics);
            _channelMetrics.Clear();
        }

        // Calculate consolidated metrics
        var totalSuccess = 0;
        var totalFailure = 0;
        var totalProcessingTime = TimeSpan.Zero;

        foreach (var channelMetric in snapshot.Values)
        {
            totalSuccess += channelMetric.SuccessCount;
            totalFailure += channelMetric.FailureCount;
            totalProcessingTime += channelMetric.TotalProcessingTime;
        }

        var totalCount = totalSuccess + totalFailure;
        var avgProcessingTime = totalProcessingTime.TotalMilliseconds / totalCount;

        // Build per-channel report
        var channelReports = snapshot
            .OrderBy(x => x.Key)
            .Select(kvp =>
            {
                var channelTotal = kvp.Value.SuccessCount + kvp.Value.FailureCount;
                var channelAvg = kvp.Value.TotalProcessingTime.TotalMilliseconds / channelTotal;
                return $"{kvp.Key}: {channelTotal} msgs ({kvp.Value.SuccessCount}✓/{kvp.Value.FailureCount}✗, {channelAvg:F2}ms avg)";
            })
            .ToList();

        var perChannelDetails = string.Join(" | ", channelReports);

        logger.LogInformation(
            "Message reception report - CONSOLIDATED: {TotalCount} messages ({SuccessCount}✓/{FailureCount}✗, {AvgProcessingTime:F2}ms avg) | PER-CHANNEL: {PerChannelDetails}",
            totalCount,
            totalSuccess,
            totalFailure,
            avgProcessingTime,
            perChannelDetails);
    }
}
