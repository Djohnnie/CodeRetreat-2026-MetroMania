namespace MetroMania.Application.Interfaces;

public interface ISubmissionQueueService
{
    Task EnqueueSubmissionAsync(Guid submissionId, CancellationToken cancellationToken = default);
}
