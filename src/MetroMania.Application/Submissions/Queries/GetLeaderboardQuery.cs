using MediatR;
using MetroMania.Application.DTOs;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Submissions.Queries;

public record GetLeaderboardQuery : IRequest<List<LeaderboardEntryDto>>;

public class GetLeaderboardQueryHandler(
    IUserRepository userRepository,
    ISubmissionRepository submissionRepository,
    ISubmissionScoreRepository scoreRepository,
    ILevelRepository levelRepository)
    : IRequestHandler<GetLeaderboardQuery, List<LeaderboardEntryDto>>
{
    public async Task<List<LeaderboardEntryDto>> Handle(GetLeaderboardQuery request, CancellationToken cancellationToken)
    {
        var users = await userRepository.GetAllAsync();
        var levels = await levelRepository.GetAllAsync();
        var stats = await submissionRepository.GetSubmissionStatsByUserAsync();

        var entries = new List<LeaderboardEntryDto>();

        foreach (var user in users)
        {
            stats.TryGetValue(user.Id, out var userStats);
            if (userStats.Count == 0)
                continue;

            // Get the latest submission for this user
            var submissions = await submissionRepository.GetByUserIdAsync(user.Id);
            var latestSubmission = submissions.FirstOrDefault();
            if (latestSubmission is null)
                continue;

            var scores = await scoreRepository.GetBySubmissionIdAsync(latestSubmission.Id);

            var levelScores = scores
                .Select(s =>
                {
                    var level = levels.FirstOrDefault(l => l.Id == s.LevelId);
                    return new SubmissionScoreDto(
                        s.LevelId,
                        level?.Title ?? "Unknown",
                        level?.SortOrder ?? 0,
                        s.Score);
                })
                .OrderBy(s => s.LevelTitle)
                .ToList();

            entries.Add(new LeaderboardEntryDto(
                user.Id,
                user.Name,
                levelScores.Sum(s => s.Score),
                userStats.Count,
                levelScores));
        }

        return entries.OrderByDescending(e => e.TotalScore).ToList();
    }
}
