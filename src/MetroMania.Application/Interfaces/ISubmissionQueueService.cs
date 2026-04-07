namespace MetroMania.Application.Interfaces;

public interface ISubmissionQueueService
{
    Task EnqueueRunAsync(Guid submissionId, CancellationToken cancellationToken = default);
    Task EnqueueRenderAsync(Guid submissionId, CancellationToken cancellationToken = default);
}
