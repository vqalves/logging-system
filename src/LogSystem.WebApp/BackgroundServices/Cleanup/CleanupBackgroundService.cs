
using LogSystem.Core.Services.Database;
using Microsoft.Data.SqlClient;

namespace LogSystem.WebApp.BackgroundServices.Cleanup;

public class CleanupBackgroundService(
    CleanupBackgroundServiceConfig cleanupConfig,
    DatabaseService databaseService,
    DatabaseConfig databaseConfig,
    ILogger<CleanupBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("CleanupBackgroundService starting...");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessCleanupCycleAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error during cleanup cycle");
                }

                // Wait 1 minute before next cycle
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("CleanupBackgroundService is stopping due to cancellation");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error in CleanupBackgroundService");
            throw;
        }
    }

    private async Task ProcessCleanupCycleAsync(CancellationToken stoppingToken)
    {
        // Fetch all LogCollections
        var logCollections = new List<LogCollection>();
        await foreach (var collection in databaseService.ListLogCollectionsAsync())
        {
            logCollections.Add(collection);
        }

        if (logCollections.Count == 0)
        {
            logger.LogDebug("No LogCollections found for cleanup");
            return;
        }

        logger.LogInformation("Processing cleanup for {Count} LogCollections", logCollections.Count);

        // Use SemaphoreSlim to limit concurrent processing
        using var semaphore = new SemaphoreSlim(cleanupConfig.MaxConcurrentCollections);
        var tasks = logCollections.Select(async collection =>
        {
            await semaphore.WaitAsync(stoppingToken);
            try
            {
                await CleanupLogCollectionAsync(collection, stoppingToken);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    private async Task CleanupLogCollectionAsync(LogCollection logCollection, CancellationToken stoppingToken)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow;
            var totalRowsDeleted = 0;

            logger.LogDebug("Starting cleanup for LogCollection {ClientId} (Table: {TableName})",
                logCollection.ClientId, logCollection.TableName);

            // TODO: Move the sql logic to the corresponding repository and call the repository here
            using var connection = new SqlConnection(databaseConfig.ConnectionString);
            await connection.OpenAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Delete in batches using DELETE TOP
                var deleteSql = $@"
                    DELETE TOP (@MaxRows)
                    FROM [logcollection].[{logCollection.TableName}]
                    WHERE [ValidUntilUtc] < @CutoffTime";

                using var command = new SqlCommand(deleteSql, connection);
                command.Parameters.AddWithValue("@MaxRows", cleanupConfig.MaxRowsPerBatch);
                command.Parameters.AddWithValue("@CutoffTime", cutoffTime);

                var rowsDeleted = await command.ExecuteNonQueryAsync(stoppingToken);
                totalRowsDeleted += rowsDeleted;

                // If we deleted fewer rows than the batch size, we're done
                if (rowsDeleted < cleanupConfig.MaxRowsPerBatch)
                {
                    break;
                }
            }

            if (totalRowsDeleted > 0)
            {
                logger.LogInformation("Cleanup completed for LogCollection {ClientId}: {RowsDeleted} rows deleted",
                    logCollection.ClientId, totalRowsDeleted);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cleaning up LogCollection {ClientId} (Table: {TableName})",
                logCollection.ClientId, logCollection.TableName);
            // Continue processing other collections despite individual failures
        }
    }
}
