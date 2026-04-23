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

                IAsyncEnumerable<ReadOnlyMemory<byte>> messageStream;

                // Check if random generation is requested
                if (random.HasValue && random.Value > 0)
                {
                    // Generate random log entries as stream
                    messageStream = StreamRandomLogsAsync(random.Value);
                }
                else
                {
                    // Stream parse JSON from request body
                    messageStream = StreamJsonArrayItemsAsync(context.Request.Body);
                }

                // Publish messages to RabbitMQ using streaming
                int count = 0;
                await foreach (var message in messageStream)
                {
                    try
                    {
                        await publisher.PublishAsync(
                            exchange: config.RabbitMqExchangeName,
                            routingKey: config.RabbitMqRoutingKey,
                            message: message);

                        count++;
                    }
                    catch (Exception ex)
                    {
                        // Return the count of messages published before the failure
                        return Results.Problem(
                            detail: $"Failed to publish message {count + 1}: {ex.Message}. Successfully published {count} messages before failure.",
                            statusCode: 500);
                    }
                }

                return Results.Ok(new { message = "Log messages published successfully", count });
            }
            catch (JsonException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    { "Body", new[] { $"Request body must be valid JSON array: {ex.Message}" } }
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("empty"))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    { "Body", new[] { ex.Message } }
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        });
    }

    private static async IAsyncEnumerable<ReadOnlyMemory<byte>> StreamJsonArrayItemsAsync(Stream bodyStream)
    {
        // Use JsonSerializer to deserialize stream as an array incrementally
        var items = JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(
            bodyStream,
            new JsonSerializerOptions { AllowTrailingCommas = true });

        bool hasItems = false;

        await foreach (var item in items)
        {
            hasItems = true;
            yield return JsonSerializer.SerializeToUtf8Bytes(item);
        }

        // Validate that at least one item was processed
        if (!hasItems)
            throw new InvalidOperationException("JSON array cannot be empty.");
    }

    private static async IAsyncEnumerable<ReadOnlyMemory<byte>> StreamRandomLogsAsync(int count)
    {
        await Task.Yield(); // Make it truly async

        for (int i = 0; i < count; i++)
        {
            var log = RandomLogGenerator.GenerateSingleLog();
            yield return JsonSerializer.SerializeToUtf8Bytes(log);
        }
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

        public static object GenerateSingleLog()
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

            // Create char array with target length
            var result = new char[targetLength];
            int position = 0;

            // Loop through positions filling with word chunks and separators
            while (position < targetLength)
            {
                // Generate random word length (3-20 characters)
                int wordLength = Random.Shared.Next(3, 21);

                // Fill word characters
                for (int i = 0; i < wordLength && position < targetLength; i++)
                {
                    result[position] = alphanumeric[Random.Shared.Next(alphanumeric.Length)];
                    position++;
                }

                // Add separator if we haven't reached the end
                if (position < targetLength)
                {
                    result[position] = separators[Random.Shared.Next(separators.Length)];
                    position++;
                }
            }

            return new string(result);
        }
    }
}
