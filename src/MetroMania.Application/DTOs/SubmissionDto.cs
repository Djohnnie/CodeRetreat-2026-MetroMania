using MetroMania.Domain.Entities;

namespace MetroMania.Application.DTOs;

public record SubmissionDto(Guid Id, int Version, string Code, DateTime SubmittedAt)
{
    public static SubmissionDto FromEntity(Submission submission) =>
        new(submission.Id, submission.Version, submission.Code, submission.SubmittedAt);
}
