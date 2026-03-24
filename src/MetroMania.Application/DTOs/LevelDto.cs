using MetroMania.Domain.Entities;

namespace MetroMania.Application.DTOs;

public record LevelDto(
    Guid Id,
    string Title,
    string Description,
    int GridWidth,
    int GridHeight,
    int SortOrder,
    DateTime CreatedAt,
    List<StationPlacement> Stations)
{
    public static LevelDto FromEntity(Level level) =>
        new(level.Id, level.Title, level.Description, level.GridWidth, level.GridHeight,
            level.SortOrder, level.CreatedAt, level.LevelData.Stations);
}
