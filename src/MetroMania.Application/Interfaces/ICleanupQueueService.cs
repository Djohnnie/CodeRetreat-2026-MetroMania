namespace MetroMania.Application.Interfaces;

public interface ICleanupQueueService
{
    Task EnqueueCleanupAsync(Guid submissionId, List<(Guid LevelId, int TotalFrames)> renders, CancellationToken cancellationToken = default);
}
