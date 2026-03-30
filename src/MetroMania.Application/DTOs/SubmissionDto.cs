using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;
using MetroMania.Domain.Extensions;

namespace MetroMania.Application.DTOs;

public record SubmissionDto(
    Guid Id,
    Guid UserId,
    int Version,
    string Code,
    SubmissionStatus Status,
    DateTime SubmittedAt,
    List<SubmissionScoreDto> Scores,
    int TotalScore)
{
    public static SubmissionDto FromEntity(Submission submission) =>
        FromEntity(submission, [], []);

    public static SubmissionDto FromEntity(
        Submission submission,
        List<SubmissionScore> scores,
        List<Level> levels)
    {
        var scoreDtos = scores
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
            .OrderBy(s => s.SortOrder)
            .ToList();

        return new SubmissionDto(
            submission.Id,
            submission.UserId,
            submission.Version,
            submission.Code.Base64Decode(),
            submission.Status,
            submission.SubmittedAt,
            scoreDtos,
            scoreDtos.Sum(s => s.Score));
    }
}
