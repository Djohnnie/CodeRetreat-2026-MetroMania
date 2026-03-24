using MediatR;
using MetroMania.Application.DTOs;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Levels.Queries;

public record GetLevelQuery(Guid Id) : IRequest<LevelDto?>;

public class GetLevelQueryHandler(ILevelRepository levelRepository)
    : IRequestHandler<GetLevelQuery, LevelDto?>
{
    public async Task<LevelDto?> Handle(GetLevelQuery request, CancellationToken cancellationToken)
    {
        var level = await levelRepository.GetByIdAsync(request.Id);
        if (level is null) return null;
        return new LevelDto(level.Id, level.Title, level.Description, level.GridWidth, level.GridHeight, level.SortOrder, level.CreatedAt, level.LevelData.Stations);
    }
}
