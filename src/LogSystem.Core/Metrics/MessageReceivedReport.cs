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
        public TimeSpan TotalReadingPayload { get; set; }
        public TimeSpan TotalExtratingCollectionName { get; set; }
        public TimeSpan TotalExtratingLog { get; set; }
        public TimeSpan TotalWritingToChannel { get; set; }
    }

    public void RecordMessage(
        string channelId,
        TimeSpan processingTime,
        bool success,
        TimeSpan readingPayload = default,
        TimeSpan extratingCollectionName = default,
        TimeSpan extratingLog = default,
        TimeSpan writingToChannel = default)
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
            metrics.TotalReadingPayload += readingPayload;
            metrics.TotalExtratingCollectionName += extratingCollectionName;
            metrics.TotalExtratingLog += extratingLog;
            metrics.TotalWritingToChannel += writingToChannel;
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
        var totalReadingPayload = TimeSpan.Zero;
        var totalExtratingCollectionName = TimeSpan.Zero;
        var totalExtratingLog = TimeSpan.Zero;
        var totalWritingToChannel = TimeSpan.Zero;

        foreach (var channelMetric in snapshot.Values)
        {
            totalSuccess += channelMetric.SuccessCount;
            totalFailure += channelMetric.FailureCount;
            totalProcessingTime += channelMetric.TotalProcessingTime;
            totalReadingPayload += channelMetric.TotalReadingPayload;
            totalExtratingCollectionName += channelMetric.TotalExtratingCollectionName;
            totalExtratingLog += channelMetric.TotalExtratingLog;
            totalWritingToChannel += channelMetric.TotalWritingToChannel;
        }

        var totalCount = totalSuccess + totalFailure;
        var avgProcessingTime = totalProcessingTime.TotalMilliseconds / totalCount;
        var avgReadingPayload = totalReadingPayload.TotalMilliseconds / totalCount;
        var avgExtratingCollectionName = totalExtratingCollectionName.TotalMilliseconds / totalCount;
        var avgExtratingLog = totalExtratingLog.TotalMilliseconds / totalCount;
        var avgWritingToChannel = totalWritingToChannel.TotalMilliseconds / totalCount;

        // Build per-channel report
        var channelReports = snapshot
            .OrderBy(x => x.Key)
            .Select(kvp =>
            {
                var channelTotal = kvp.Value.SuccessCount + kvp.Value.FailureCount;
                var channelAvg = kvp.Value.TotalProcessingTime.TotalMilliseconds / channelTotal;
                return $"{kvp.Key}: {kvp.Value.SuccessCount}✓/{kvp.Value.FailureCount}✗";
            })
            .ToList();

        var perChannelDetails = string.Join(", ", channelReports);

        logger.LogInformation(
            "Message reception report - Msg={SuccessCount}✓/{FailureCount}✗, AvgTotal={AvgProcessingTime:F2}ms, ReadPayload={AvgReadingPayload:F2}ms, ExtractCol={AvgExtratingCollectionName:F2}ms, ExtractLog={AvgExtratingLog:F2}ms, WriteChannel={AvgWritingToChannel:F2}ms " +
            "| PER-CHANNEL: {PerChannelDetails}",
            totalSuccess,
            totalFailure,
            avgProcessingTime,
            avgReadingPayload,
            avgExtratingCollectionName,
            avgExtratingLog,
            avgWritingToChannel,
            perChannelDetails);
    }
}
