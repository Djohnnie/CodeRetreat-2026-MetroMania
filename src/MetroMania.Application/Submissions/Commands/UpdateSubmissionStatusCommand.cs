using MediatR;
using MetroMania.Domain.Enums;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Submissions.Commands;

public record UpdateSubmissionStatusCommand(Guid SubmissionId, SubmissionStatus Status) : IRequest;

public class UpdateSubmissionStatusCommandHandler(ISubmissionRepository submissionRepository)
    : IRequestHandler<UpdateSubmissionStatusCommand>
{
    public async Task Handle(UpdateSubmissionStatusCommand request, CancellationToken cancellationToken)
    {
        var submission = await submissionRepository.GetByIdAsync(request.SubmissionId)
            ?? throw new InvalidOperationException($"Submission {request.SubmissionId} not found.");

        submission.Status = request.Status;
        await submissionRepository.UpdateAsync(submission);
    }
}
