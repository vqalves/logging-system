using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using LogSystem.Core.Services.Common;
using LogSystem.Core.Services.Common.Compression;

namespace LogSystem.WebApp.Endpoints.LogDataEndpoints;

public static class TestCompressionEndpoint
{
    private const string Route = "/api/log/test-compression";
    private static readonly string[] EmptyBodyError = new[] { "Request body is required and cannot be empty." };

    public static string UrlForTestCompression() => Route;

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(Route, async (
            HttpContext context,
            CompressionFactory compressionFactory,
            string? format = "json") =>
        {
            try
            {
                // Read the request body as bytes
                using var memoryStream = new MemoryStream();
                await context.Request.Body.CopyToAsync(memoryStream);
                var bodyBytes = memoryStream.ToArray();

                // Validate that body is not empty
                if (bodyBytes.Length == 0)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        { "Body", EmptyBodyError }
                    });
                }

                var originalSize = bodyBytes.Length;
                var report = new CompressionReport();

                // Add uncompressed baseline as first item
                report.Results.Add(new CompressionReportItem
                {
                    CompressionStrategy = "Uncompressed",
                    CompressionLevel = "N/A",
                    CompressionDuration = TimeSpan.Zero,
                    CompressedBytesSize = originalSize,
                    SizeComparedWithUncompressed = 1.0,
                    ReducedBytesPerSecondOfExecution = 0
                });

                // Test Brotli (all CompressionLevel enum values)
                foreach (CompressionLevel level in Enum.GetValues<CompressionLevel>())
                {
                    var strategy = new BrotliCompressionStrategy(level);
                    var result = TestCompression(
                        () => strategy.Compress(bodyBytes),
                        "Brotli",
                        level.ToString(),
                        originalSize);
                    report.Results.Add(result);
                }

                // Test Gzip (all CompressionLevel enum values)
                foreach (CompressionLevel level in Enum.GetValues<CompressionLevel>())
                {
                    var strategy = new GzipCompressionStrategy(level);
                    var result = TestCompression(
                        () => strategy.Compress(bodyBytes),
                        "Gzip",
                        level.ToString(),
                        originalSize);
                    report.Results.Add(result);
                }

                // Test Deflate (all CompressionLevel enum values)
                foreach (CompressionLevel level in Enum.GetValues<CompressionLevel>())
                {
                    var strategy = new DeflateCompressionStrategy(level);
                    var result = TestCompression(
                        () => strategy.Compress(bodyBytes),
                        "Deflate",
                        level.ToString(),
                        originalSize);
                    report.Results.Add(result);
                }

                // Test Zstandard (levels 1-22)
                for (int level = 1; level <= 22; level++)
                {
                    var strategy = new ZstdCompressionStrategy(level);
                    var result = TestCompression(
                        () => strategy.Compress(bodyBytes),
                        "Zstandard",
                        level.ToString(),
                        originalSize);
                    report.Results.Add(result);
                }

                // Return based on requested format
                var formatLower = format?.ToLowerInvariant() ?? "json";

                if (formatLower == "csv")
                {
                    var csv = GenerateCsv(report);
                    return Results.Text(csv, "text/csv");
                }

                return Results.Ok(report);
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        });
    }

    private static string GenerateCsv(CompressionReport report)
    {
        var sb = new StringBuilder();

        // Add header row
        sb.AppendLine("CompressionStrategy,CompressionLevel,CompressionDuration,CompressedBytesSize,SizeComparedWithUncompressed,ReducedBytesPerSecondOfExecution");

        // Add data rows
        foreach (var item in report.Results)
        {
            sb.AppendLine($"{EscapeCsvField(item.CompressionStrategy)},{EscapeCsvField(item.CompressionLevel)},{item.CompressionDuration.TotalMilliseconds},{item.CompressedBytesSize},{item.SizeComparedWithUncompressed},{item.ReducedBytesPerSecondOfExecution}");
        }

        return sb.ToString();
    }

    private static string EscapeCsvField(string field)
    {
        // If field contains comma, quote, or newline, wrap in quotes and escape internal quotes
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }

    private static CompressionReportItem TestCompression(
        Func<byte[]> compressionFunc,
        string strategy,
        string level,
        long originalSize)
    {
        // Run 3 times and keep the last result
        byte[]? compressedBytes = null;
        TimeSpan duration = TimeSpan.Zero;

        for (int i = 0; i < 10; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            compressedBytes = compressionFunc();
            stopwatch.Stop();
            duration = stopwatch.Elapsed;
        }

        var compressedSize = compressedBytes!.Length;
        var sizeRatio = (double)compressedSize / originalSize;
        var bytesReduced = originalSize - compressedSize;
        var reducedBytesPerSecond = duration.TotalSeconds > 0
            ? bytesReduced / duration.TotalSeconds
            : 0;

        return new CompressionReportItem
        {
            CompressionStrategy = strategy,
            CompressionLevel = level,
            CompressionDuration = duration,
            CompressedBytesSize = compressedSize,
            SizeComparedWithUncompressed = sizeRatio,
            ReducedBytesPerSecondOfExecution = reducedBytesPerSecond
        };
    }

    public class CompressionReport
    {
        public List<CompressionReportItem> Results { get; set; } = [];
    }

    public class CompressionReportItem
    {
        public string CompressionStrategy { get; set; } = string.Empty;
        public string CompressionLevel { get; set; } = string.Empty;
        public TimeSpan CompressionDuration { get; set; }
        public long CompressedBytesSize { get; set; }
        public double SizeComparedWithUncompressed { get; set; }
        public double ReducedBytesPerSecondOfExecution { get; set; }
    }
}
