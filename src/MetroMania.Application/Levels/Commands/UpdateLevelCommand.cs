using MediatR;
using MetroMania.Application.DTOs;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Levels.Commands;

public record UpdateLevelCommand(Guid Id, string Title, string Description, int GridWidth, int GridHeight) : IRequest<LevelDto?>;

public class UpdateLevelCommandHandler(ILevelRepository levelRepository)
    : IRequestHandler<UpdateLevelCommand, LevelDto?>
{
    public async Task<LevelDto?> Handle(UpdateLevelCommand request, CancellationToken cancellationToken)
    {
        var level = await levelRepository.GetByIdAsync(request.Id);
        if (level is null) return null;

        level.Title = request.Title;
        level.Description = request.Description;
        level.GridWidth = request.GridWidth;
        level.GridHeight = request.GridHeight;
        await levelRepository.UpdateAsync(level);

        return new LevelDto(level.Id, level.Title, level.Description, level.GridWidth, level.GridHeight, level.SortOrder, level.CreatedAt, level.LevelData.Stations);
    }
}
