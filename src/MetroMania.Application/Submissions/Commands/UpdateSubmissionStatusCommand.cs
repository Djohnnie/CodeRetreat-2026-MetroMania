using MediatR;
using MetroMania.Domain.Enums;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Submissions.Commands;

public record UpdateSubmissionStatusCommand(
    Guid SubmissionId,
    RunStatus? RunStatus = null,
    RenderStatus? RenderStatus = null,
    string? Message = null) : IRequest;

public class UpdateSubmissionStatusCommandHandler(ISubmissionRepository submissionRepository)
    : IRequestHandler<UpdateSubmissionStatusCommand>
{
    public async Task Handle(UpdateSubmissionStatusCommand request, CancellationToken cancellationToken)
    {
        await submissionRepository.UpdateStatusFieldsAsync(
            request.SubmissionId, request.RunStatus, request.RenderStatus, request.Message);
    }
}
