using MediatR;
using MetroMania.Application.DTOs;
using MetroMania.Domain.Entities;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Submissions.Commands;

public record SubmitCodeCommand(Guid UserId, string Code) : IRequest<SubmissionDto>;

public class SubmitCodeCommandHandler(ISubmissionRepository submissionRepository)
    : IRequestHandler<SubmitCodeCommand, SubmissionDto>
{
    public async Task<SubmissionDto> Handle(SubmitCodeCommand request, CancellationToken cancellationToken)
    {
        var nextVersion = await submissionRepository.GetNextVersionAsync(request.UserId);

        var submission = new Submission
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Version = nextVersion,
            Code = request.Code,
            SubmittedAt = DateTime.UtcNow
        };

        await submissionRepository.AddAsync(submission);

        return SubmissionDto.FromEntity(submission);
    }
}
