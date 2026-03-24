using MediatR;
using MetroMania.Application.DTOs;
using MetroMania.Domain.Entities;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Levels.Commands;

public record UpdateGridDataCommand(Guid LevelId, List<StationPlacement> Stations) : IRequest<LevelDto?>;

public class UpdateGridDataCommandHandler(ILevelRepository levelRepository)
    : IRequestHandler<UpdateGridDataCommand, LevelDto?>
{
    public async Task<LevelDto?> Handle(UpdateGridDataCommand request, CancellationToken cancellationToken)
    {
        var level = await levelRepository.GetByIdAsync(request.LevelId);
        if (level is null) return null;

        var valid = request.Stations.Where(s =>
            s.GridX >= 0 && s.GridX < level.GridWidth &&
            s.GridY >= 0 && s.GridY < level.GridHeight).ToList();

        level.LevelData.Stations = valid;
        await levelRepository.UpdateAsync(level);

        return LevelDto.FromEntity(level);
    }
}
