namespace MetroMania.Application.DTOs;

public record LeaderboardEntryDto(
    Guid UserId,
    string UserName,
    int TotalScore,
    int SubmissionCount,
    List<SubmissionScoreDto> LevelScores);
