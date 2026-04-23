using System.Text;
using System.Text.Json;
using LogSystem.WebApp.Services;

namespace LogSystem.WebApp.Endpoints.LogDataEndpoints;

public static class AddLogBatchEndpoint
{
    private const string Route = "/api/log/add-batch";

    public static string UrlForAddLogBatch() => Route;

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(Route, async (
            HttpContext context,
            RabbitMqPublisher publisher,
            PublishServiceConfig config,
            string? format,
            int? random) =>
        {
            try
            {
                // Validate format parameter
                if (string.IsNullOrEmpty(format) || format.ToLowerInvariant() != "json")
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        { "format", new[] { "The 'format' query parameter must be 'json'. Other formats are not supported." } }
                    });
                }

                byte[] bodyBytes;

                // Check if random generation is requested
                if (random.HasValue && random.Value > 0)
                {
                    // Generate random log entries
                    var randomLogs = RandomLogGenerator.Generate(random.Value);
                    bodyBytes = JsonSerializer.SerializeToUtf8Bytes(randomLogs);
                }
                else
                {
                    // Read the request body as bytes
                    using var memoryStream = new MemoryStream();
                    await context.Request.Body.CopyToAsync(memoryStream);
                    bodyBytes = memoryStream.ToArray();

                    // Validate that body is not empty
                    if (bodyBytes.Length == 0)
                    {
                        return Results.ValidationProblem(new Dictionary<string, string[]>
                        {
                            { "Body", new[] { "Request body is required and cannot be empty." } }
                        });
                    }
                }

                // Deserialize as JSON array
                JsonElement jsonArray;
                try
                {
                    jsonArray = JsonSerializer.Deserialize<JsonElement>(bodyBytes);
                }
                catch (JsonException ex)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        { "Body", new[] { $"Request body must be valid JSON: {ex.Message}" } }
                    });
                }

                // Validate it's an array
                if (jsonArray.ValueKind != JsonValueKind.Array)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        { "Body", new[] { "Request body must be a JSON array." } }
                    });
                }

                // Validate array is not empty
                var arrayLength = jsonArray.GetArrayLength();
                if (arrayLength == 0)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        { "Body", new[] { "JSON array cannot be empty." } }
                    });
                }

                // Publish each item to RabbitMQ
                int count = 0;
                foreach (var item in jsonArray.EnumerateArray())
                {
                    // Serialize each item to bytes
                    var itemBytes = JsonSerializer.SerializeToUtf8Bytes(item);

                    // Publish to RabbitMQ
                    await publisher.PublishAsync(
                        exchange: config.RabbitMqExchangeName,
                        routingKey: config.RabbitMqRoutingKey,
                        message: itemBytes);

                    count++;
                }

                return Results.Ok(new { message = "Log messages published successfully", count });
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        });
    }

    private static class RandomLogGenerator
    {
        private static readonly string[] LogLevels = { "Debug", "Information", "Warning", "Error", "Critical" };
        private static readonly string[] ClientIds =
        {
            "Attendance", "Payroll", "Inventory", "Sales", "Marketing",
            "Support", "Analytics", "Billing", "Notifications", "Reports"
        };
        private static readonly string[] ExceptionTypes =
        {
            "System.NullReferenceException",
            "System.ArgumentException",
            "System.ArgumentNullException",
            "System.InvalidOperationException",
            "System.IndexOutOfRangeException",
            "System.DivideByZeroException",
            "System.FormatException",
            "System.NotImplementedException",
            "System.TimeoutException",
            "System.UnauthorizedAccessException"
        };

        public static IEnumerable<object> Generate(int count)
        {
            var logs = new List<object>(count);

            for (int i = 0; i < count; i++)
            {
                logs.Add(GenerateSingleLog());
            }

            return logs;
        }

        private static object GenerateSingleLog()
        {
            var timestamp = GenerateRandomTimestamp();
            var level = LogLevels[Random.Shared.Next(LogLevels.Length)];
            var message = GenerateRandomMessage(20000);
            var clientId = ClientIds[Random.Shared.Next(ClientIds.Length)];
            var includeException = Random.Shared.Next(100) < 10; // 10% chance

            if (includeException)
            {
                return new
                {
                    Timestamp = timestamp,
                    Level = level,
                    Message = message,
                    Properties = new { LogCollectionClientId = clientId },
                    Exception = new { Message = ExceptionTypes[Random.Shared.Next(ExceptionTypes.Length)] }
                };
            }
            else
            {
                return new
                {
                    Timestamp = timestamp,
                    Level = level,
                    Message = message,
                    Properties = new { LogCollectionClientId = clientId }
                };
            }
        }

        private static string GenerateRandomTimestamp()
        {
            var start = new DateTime(2026, 1, 1);
            var end = new DateTime(2026, 12, 31, 23, 59, 59);
            var range = (end - start).TotalSeconds;
            var randomSeconds = Random.Shared.NextDouble() * range;
            var randomDate = start.AddSeconds(randomSeconds);
            return randomDate.ToString("yyyy-MM-ddTHH:mm:ss");
        }

        private static string GenerateRandomMessage(int targetLength)
        {
            const string alphanumeric = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var separators = new[] { ' ', ',', '.' };

            // Calculate average word length (3-20 range, midpoint ~11.5)
            const double averageWordLength = 11.5;

            // Account for separators in the calculation (1 char per separator)
            // Formula: targetLength = (wordCount * avgWordLength) + (wordCount - 1) * 1
            int estimatedWordCount = (int)Math.Ceiling(targetLength / (averageWordLength + 1));

            // Generate all words
            var words = new string[estimatedWordCount * 2];
            for (int i = 0; i < estimatedWordCount; i += 2)
            {
                int wordLength = Random.Shared.Next(3, 21);
                var wordChars = new char[wordLength];

                for (int j = 0; j < wordLength; j++)
                    wordChars[j] = alphanumeric[Random.Shared.Next(alphanumeric.Length)];

                words[i] = new string(wordChars);
                words[i + 1] = separators[Random.Shared.Next(separators.Length)].ToString();
            }

            return string.Concat(words);
        }
    }
}
