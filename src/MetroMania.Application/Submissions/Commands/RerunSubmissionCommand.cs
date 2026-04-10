using MediatR;
using MetroMania.Application.Interfaces;
using MetroMania.Domain.Enums;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Submissions.Commands;

public record RerunSubmissionCommand(Guid SubmissionId) : IRequest<bool>;

public class RerunSubmissionCommandHandler(
    ISubmissionRepository submissionRepository,
    ISubmissionQueueService submissionQueueService)
    : IRequestHandler<RerunSubmissionCommand, bool>
{
    public async Task<bool> Handle(RerunSubmissionCommand request, CancellationToken cancellationToken)
    {
        var submission = await submissionRepository.GetByIdAsync(request.SubmissionId);
        if (submission is null)
            return false;

        await submissionRepository.UpdateStatusFieldsAsync(
            request.SubmissionId,
            RunStatus.Waiting,
            RenderStatus.Waiting,
            null);

        await submissionQueueService.EnqueueRunAsync(request.SubmissionId, cancellationToken);
        await submissionQueueService.EnqueueRenderAsync(request.SubmissionId, cancellationToken);

        return true;
    }
}
