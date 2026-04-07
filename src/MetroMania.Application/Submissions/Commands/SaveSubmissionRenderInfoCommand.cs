using MediatR;
using MetroMania.Domain.Entities;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Submissions.Commands;

/// <summary>
/// Saves a single SubmissionRender row per level/submission, storing only the TotalFrames count.
/// Blob names are derived from the (SubmissionId, LevelId, Hour) pattern.
/// </summary>
public record SaveSubmissionRenderInfoCommand(Guid SubmissionId, Guid LevelId, int TotalFrames) : IRequest;

public class SaveSubmissionRenderInfoCommandHandler(ISubmissionRenderRepository renderRepository)
    : IRequestHandler<SaveSubmissionRenderInfoCommand>
{
    public async Task Handle(SaveSubmissionRenderInfoCommand request, CancellationToken cancellationToken)
    {
        var existing = await renderRepository.GetBySubmissionAndLevelAsync(request.SubmissionId, request.LevelId);
        if (existing is not null)
        {
            existing.TotalFrames = request.TotalFrames;
            await renderRepository.UpdateAsync(existing);
            return;
        }

        await renderRepository.AddAsync(new SubmissionRender
        {
            Id = Guid.NewGuid(),
            SubmissionId = request.SubmissionId,
            LevelId = request.LevelId,
            TotalFrames = request.TotalFrames
        });
    }
}
