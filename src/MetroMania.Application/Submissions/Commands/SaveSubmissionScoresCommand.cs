using MediatR;
using MetroMania.Domain.Entities;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Submissions.Commands;

public record SaveSubmissionScoresCommand(Guid SubmissionId, List<SaveSubmissionScoresCommand.LevelScore> Scores) : IRequest
{
    public record LevelScore(Guid LevelId, int Score);
}

public class SaveSubmissionScoresCommandHandler(ISubmissionScoreRepository scoreRepository)
    : IRequestHandler<SaveSubmissionScoresCommand>
{
    public async Task Handle(SaveSubmissionScoresCommand request, CancellationToken cancellationToken)
    {
        var entities = request.Scores.Select(s => new SubmissionScore
        {
            Id = Guid.NewGuid(),
            SubmissionId = request.SubmissionId,
            LevelId = s.LevelId,
            Score = s.Score
        });

        await scoreRepository.AddManyAsync(entities);
    }
}
