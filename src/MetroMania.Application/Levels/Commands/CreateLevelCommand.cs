using MediatR;
using MetroMania.Application.DTOs;
using MetroMania.Domain.Entities;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Levels.Commands;

public record CreateLevelCommand(string Title, string Description) : IRequest<LevelDto>;

public class CreateLevelCommandHandler(ILevelRepository levelRepository)
    : IRequestHandler<CreateLevelCommand, LevelDto>
{
    public async Task<LevelDto> Handle(CreateLevelCommand request, CancellationToken cancellationToken)
    {
        var maxOrder = await levelRepository.GetMaxSortOrderAsync();

        var level = new Level
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            SortOrder = maxOrder + 1
        };

        await levelRepository.AddAsync(level);
        return new LevelDto(level.Id, level.Title, level.Description, level.SortOrder, level.CreatedAt);
    }
}
