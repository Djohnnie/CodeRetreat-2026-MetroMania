using MediatR;
using MetroMania.Application.DTOs;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Levels.Queries;

public record GetAllLevelsQuery : IRequest<List<LevelDto>>;

public class GetAllLevelsQueryHandler(ILevelRepository levelRepository)
    : IRequestHandler<GetAllLevelsQuery, List<LevelDto>>
{
    public async Task<List<LevelDto>> Handle(GetAllLevelsQuery request, CancellationToken cancellationToken)
    {
        var levels = await levelRepository.GetAllAsync();
        return levels.Select(l => new LevelDto(l.Id, l.Title, l.Description, l.SortOrder, l.CreatedAt)).ToList();
    }
}
