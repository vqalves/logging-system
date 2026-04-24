
namespace LogSystem.Core.BackgroundServices.Cleanup;

public class CleanupBackgroundServiceConfig
{
    public required int MaxRowsPerBatch { get; init; }
    public required int MaxConcurrentCollections { get; init; }
}
