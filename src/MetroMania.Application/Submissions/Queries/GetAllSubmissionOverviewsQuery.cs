using MediatR;
using MetroMania.Application.DTOs;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Submissions.Queries;

public record GetAllSubmissionOverviewsQuery : IRequest<List<UserSubmissionOverviewDto>>;

public class GetAllSubmissionOverviewsQueryHandler(
    IUserRepository userRepository,
    ISubmissionRepository submissionRepository)
    : IRequestHandler<GetAllSubmissionOverviewsQuery, List<UserSubmissionOverviewDto>>
{
    public async Task<List<UserSubmissionOverviewDto>> Handle(GetAllSubmissionOverviewsQuery request, CancellationToken cancellationToken)
    {
        var users = await userRepository.GetAllAsync();
        var stats = await submissionRepository.GetSubmissionStatsByUserAsync();

        return users.Select(u =>
        {
            stats.TryGetValue(u.Id, out var userStats);
            return new UserSubmissionOverviewDto(
                u.Id,
                u.Name,
                userStats.Count,
                userStats.LastSubmittedAt);
        }).ToList();
    }
}
