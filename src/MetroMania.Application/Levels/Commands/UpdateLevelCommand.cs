using MediatR;
using MetroMania.Application.DTOs;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Levels.Commands;

public record UpdateLevelCommand(Guid Id, string Title, string Description) : IRequest<LevelDto?>;

public class UpdateLevelCommandHandler(ILevelRepository levelRepository)
    : IRequestHandler<UpdateLevelCommand, LevelDto?>
{
    public async Task<LevelDto?> Handle(UpdateLevelCommand request, CancellationToken cancellationToken)
    {
        var level = await levelRepository.GetByIdAsync(request.Id);
        if (level is null) return null;

        level.Title = request.Title;
        level.Description = request.Description;
        await levelRepository.UpdateAsync(level);

        return new LevelDto(level.Id, level.Title, level.Description, level.SortOrder, level.CreatedAt);
    }
}
