using MediatR;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Levels.Commands;

public record DeleteLevelCommand(Guid Id) : IRequest<bool>;

public class DeleteLevelCommandHandler(
    ILevelRepository levelRepository,
    ISubmissionScoreRepository submissionScoreRepository,
    ISubmissionRenderRepository submissionRenderRepository)
    : IRequestHandler<DeleteLevelCommand, bool>
{
    public async Task<bool> Handle(DeleteLevelCommand request, CancellationToken cancellationToken)
    {
        var level = await levelRepository.GetByIdAsync(request.Id);
        if (level is null) return false;

        var allLevels = await levelRepository.GetAllAsync();

        // Remove all dependent rows before deleting the level
        await submissionScoreRepository.DeleteByLevelIdAsync(request.Id);
        await submissionRenderRepository.DeleteByLevelIdAsync(request.Id);

        await levelRepository.DeleteAsync(request.Id);

        // Re-sequence remaining levels
        var remaining = allLevels
            .Where(l => l.Id != request.Id)
            .OrderBy(l => l.SortOrder)
            .ToList();

        for (var i = 0; i < remaining.Count; i++)
            remaining[i].SortOrder = i;

        if (remaining.Count > 0)
            await levelRepository.UpdateManyAsync(remaining);

        return true;
    }
}
