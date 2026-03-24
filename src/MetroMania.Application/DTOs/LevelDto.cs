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
    string BackgroundColor,
    string WaterColor,
    List<StationPlacement> Stations,
    List<WaterTile> WaterTiles)
{
    public static LevelDto FromEntity(Level level) =>
        new(level.Id, level.Title, level.Description, level.GridWidth, level.GridHeight,
            level.SortOrder, level.CreatedAt,
            level.LevelData.BackgroundColor, level.LevelData.WaterColor,
            level.LevelData.Stations, level.LevelData.WaterTiles);
}
