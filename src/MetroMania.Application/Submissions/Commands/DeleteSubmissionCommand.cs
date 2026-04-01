using MediatR;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Submissions.Commands;

public record DeleteSubmissionCommand(Guid SubmissionId) : IRequest;

public class DeleteSubmissionCommandHandler(ISubmissionRepository submissionRepository)
    : IRequestHandler<DeleteSubmissionCommand>
{
    public async Task Handle(DeleteSubmissionCommand request, CancellationToken cancellationToken) =>
        await submissionRepository.DeleteAsync(request.SubmissionId);
}
