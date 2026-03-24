using MediatR;
using MetroMania.Application.DTOs;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Submissions.Queries;

public record GetUserSubmissionsQuery(Guid UserId) : IRequest<List<SubmissionDto>>;

public class GetUserSubmissionsQueryHandler(ISubmissionRepository submissionRepository)
    : IRequestHandler<GetUserSubmissionsQuery, List<SubmissionDto>>
{
    public async Task<List<SubmissionDto>> Handle(GetUserSubmissionsQuery request, CancellationToken cancellationToken)
    {
        var submissions = await submissionRepository.GetByUserIdAsync(request.UserId);
        return submissions.Select(SubmissionDto.FromEntity).ToList();
    }
}
