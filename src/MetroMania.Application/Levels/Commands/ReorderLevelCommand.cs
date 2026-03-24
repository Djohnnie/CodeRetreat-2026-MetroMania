using MediatR;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Levels.Commands;

public record ReorderLevelCommand(Guid Id, int Direction) : IRequest<bool>;

public class ReorderLevelCommandHandler(ILevelRepository levelRepository)
    : IRequestHandler<ReorderLevelCommand, bool>
{
    public async Task<bool> Handle(ReorderLevelCommand request, CancellationToken cancellationToken)
    {
        var levels = await levelRepository.GetAllAsync();
        var index = levels.FindIndex(l => l.Id == request.Id);
        if (index < 0) return false;

        var targetIndex = index + request.Direction;
        if (targetIndex < 0 || targetIndex >= levels.Count) return false;

        // Swap sort orders
        (levels[index].SortOrder, levels[targetIndex].SortOrder) =
            (levels[targetIndex].SortOrder, levels[index].SortOrder);

        await levelRepository.UpdateManyAsync([levels[index], levels[targetIndex]]);
        return true;
    }
}
