using MediatR;
using MetroMania.Application.DTOs;
using MetroMania.Domain.Enums;
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

            // Fetch all completed submissions (run finished) and their scores
            var submissions = await submissionRepository.GetByUserIdAsync(user.Id);
            var completedSubmissions = submissions
                .Where(s => s.RunStatus == RunStatus.Ran)
                .ToList();

            if (completedSubmissions.Count == 0)
                continue;

            var allScores = await scoreRepository.GetBySubmissionIdsAsync(
                completedSubmissions.Select(s => s.Id));

            // Pick the submission with the highest total score
            var bestSubmission = completedSubmissions.MaxBy(s =>
                allScores.Where(sc => sc.SubmissionId == s.Id).Sum(sc => sc.Score))!;

            var levelScores = allScores
                .Where(sc => sc.SubmissionId == bestSubmission.Id)
                .Select(s =>
                {
                    var level = levels.FirstOrDefault(l => l.Id == s.LevelId);
                    var localizedTitles = level?.LevelData.LocalizedContent
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Title)
                        ?? [];
                    var localizedDescriptions = level?.LevelData.LocalizedContent
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Description)
                        ?? [];
                    return new SubmissionScoreDto(
                        s.LevelId,
                        level?.Title ?? "Unknown",
                        level?.Description ?? "",
                        level?.SortOrder ?? 0,
                        s.Score,
                        localizedTitles,
                        localizedDescriptions);
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
