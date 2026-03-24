using MetroMania.Domain.Enums;

namespace MetroMania.Domain.Entities;

/// <summary>
/// Root container for all level grid data, serialized as JSON in the database.
/// </summary>
public class LevelData
{
    public List<StationPlacement> Stations { get; set; } = [];
}

/// <summary>
/// Represents a station placed on a level grid.
/// </summary>
public class StationPlacement
{
    public int GridX { get; set; }
    public int GridY { get; set; }
    public StationType StationType { get; set; }
}
