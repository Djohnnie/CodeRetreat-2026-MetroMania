using MediatR;
using MetroMania.Application.DTOs;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Submissions.Queries;

public record GetUserSubmissionsQuery(Guid UserId) : IRequest<List<SubmissionDto>>;

public class GetUserSubmissionsQueryHandler(
    ISubmissionRepository submissionRepository,
    ISubmissionScoreRepository scoreRepository,
    ILevelRepository levelRepository)
    : IRequestHandler<GetUserSubmissionsQuery, List<SubmissionDto>>
{
    public async Task<List<SubmissionDto>> Handle(GetUserSubmissionsQuery request, CancellationToken cancellationToken)
    {
        var submissions = await submissionRepository.GetByUserIdAsync(request.UserId);
        if (submissions.Count == 0)
            return [];

        var submissionIds = submissions.Select(s => s.Id).ToList();
        var allScores = await scoreRepository.GetBySubmissionIdsAsync(submissionIds);
        var levels = await levelRepository.GetAllAsync();

        var scoresBySubmission = allScores
            .GroupBy(s => s.SubmissionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return submissions
            .Select(s => SubmissionDto.FromEntity(
                s,
                scoresBySubmission.GetValueOrDefault(s.Id, []),
                levels))
            .ToList();
    }
}
