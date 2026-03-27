namespace MetroMania.Application.DTOs;

public record SubmissionScoreDto(Guid LevelId, string LevelTitle, int SortOrder, int Score);
