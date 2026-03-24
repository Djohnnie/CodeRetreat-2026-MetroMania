using MetroMania.Domain.Enums;

namespace MetroMania.Domain.Entities;

/// <summary>
/// Root container for all level grid data, serialized as JSON in the database.
/// </summary>
public class LevelData
{
    public string BackgroundColor { get; set; } = "#e8f5e9";
    public string WaterColor { get; set; } = "#90caf9";
    public List<StationPlacement> Stations { get; set; } = [];
    public List<WaterTile> WaterTiles { get; set; } = [];
}

/// <summary>
/// Represents a station placed on a level grid.
/// </summary>
public class StationPlacement
{
    public int GridX { get; set; }
    public int GridY { get; set; }
    public StationType StationType { get; set; }
    public int SpawnOrder { get; set; }
    public int SpawnDelayDays { get; set; }
    public StationType Shape { get; set; }
    public int SpawnFrequencyMinutes { get; set; } = 5;

    /// <summary>
    /// Seed for deterministic passenger spawning so every playthrough is identical.
    /// </summary>
    public int SpawnSeed { get; set; }
}

/// <summary>
/// Represents a water tile on the level grid.
/// </summary>
public class WaterTile
{
    public int GridX { get; set; }
    public int GridY { get; set; }
}
