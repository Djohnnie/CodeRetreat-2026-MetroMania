using MediatR;
using MetroMania.Application.DTOs;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Submissions.Queries;

public record GetSubmissionByIdQuery(Guid Id) : IRequest<SubmissionDto?>;

public class GetSubmissionByIdQueryHandler(ISubmissionRepository submissionRepository)
    : IRequestHandler<GetSubmissionByIdQuery, SubmissionDto?>
{
    public async Task<SubmissionDto?> Handle(GetSubmissionByIdQuery request, CancellationToken cancellationToken)
    {
        var submission = await submissionRepository.GetByIdAsync(request.Id);
        return submission is null ? null : SubmissionDto.FromEntity(submission);
    }
}
