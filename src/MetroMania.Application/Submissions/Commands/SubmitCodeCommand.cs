using MediatR;
using MetroMania.Application.DTOs;
using MetroMania.Domain.Entities;
using MetroMania.Domain.Extensions;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Submissions.Commands;

public record SubmitCodeCommand(Guid UserId, string Code) : IRequest<SubmissionDto>;

public class SubmitCodeCommandHandler(
    ISubmissionRepository submissionRepository,
    ISubmissionScoreRepository scoreRepository,
    ILevelRepository levelRepository)
    : IRequestHandler<SubmitCodeCommand, SubmissionDto>
{
    public async Task<SubmissionDto> Handle(SubmitCodeCommand request, CancellationToken cancellationToken)
    {
        var nextVersion = await submissionRepository.GetNextVersionAsync(request.UserId);

        var submission = new Submission
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Version = nextVersion,
            Code = request.Code.Base64Encode(),
            SubmittedAt = DateTime.UtcNow
        };

        await submissionRepository.AddAsync(submission);

        // Run submission against all levels and store scores
        var levels = await levelRepository.GetAllAsync();
        var random = new Random(submission.Id.GetHashCode());

        var scores = levels.Select(level => new SubmissionScore
        {
            Id = Guid.NewGuid(),
            SubmissionId = submission.Id,
            LevelId = level.Id,
            Score = random.Next(0, 10001) // Hardcoded random score 0–10000 for now
        }).ToList();

        if (scores.Count > 0)
            await scoreRepository.AddManyAsync(scores);

        return SubmissionDto.FromEntity(submission, scores, levels);
    }
}
