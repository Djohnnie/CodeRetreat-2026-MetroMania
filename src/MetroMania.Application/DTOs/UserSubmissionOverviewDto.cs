namespace MetroMania.Application.DTOs;

public record UserSubmissionOverviewDto(Guid UserId, string UserName, int SubmissionCount, DateTime? LastSubmissionAt);
