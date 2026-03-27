using MetroMania.Domain.Entities;

namespace MetroMania.Application.DTOs;

public record SubmissionDto(
    Guid Id,
    int Version,
    string Code,
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
                return new SubmissionScoreDto(
                    s.LevelId,
                    level?.Title ?? "Unknown",
                    level?.SortOrder ?? 0,
                    s.Score);
            })
            .OrderBy(s => s.SortOrder)
            .ToList();

        return new SubmissionDto(
            submission.Id,
            submission.Version,
            submission.Code,
            submission.SubmittedAt,
            scoreDtos,
            scoreDtos.Sum(s => s.Score));
    }
}
