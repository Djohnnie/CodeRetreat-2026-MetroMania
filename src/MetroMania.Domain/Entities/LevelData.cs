using MetroMania.Domain.Enums;

namespace MetroMania.Domain.Entities;

/// <summary>
/// Root container for all level grid data, serialized as JSON in the database.
/// </summary>
public class LevelData
{
    public string BackgroundColor { get; set; } = "#e8f5e9";
    public string WaterColor { get; set; } = "#90caf9";

    /// <summary>
    /// Seed used by the engine for all random activities (passenger spawning, weekly gifts, etc.)
    /// so every playthrough of this level is deterministic and reproducible.
    /// </summary>
    public int Seed { get; set; }

    public List<MetroStation> Stations { get; set; } = [];
    public List<Water> WaterTiles { get; set; } = [];
}

/// <summary>
/// Represents a station placed on a level grid.
/// </summary>
public class MetroStation
{
    public int GridX { get; set; }
    public int GridY { get; set; }
    public StationType StationType { get; set; }
    public int SpawnOrder { get; set; }
    public int SpawnDelayDays { get; set; }

    /// <summary>
    /// Defines how passenger spawn frequency evolves over time for this station.
    /// Each phase specifies a day threshold (relative to when the station appeared) and a spawn frequency.
    /// The station starts without spawning passengers. Once the first phase's day threshold is reached,
    /// passengers begin spawning at that frequency. As subsequent thresholds are reached, the frequency
    /// changes accordingly (typically increasing, i.e. shorter intervals).
    /// Phases should be ordered by <see cref="PassengerSpawnPhase.AfterDays"/> ascending.
    /// </summary>
    public List<PassengerSpawnPhase> PassengerSpawnPhases { get; set; } = [];
}

/// <summary>
/// Defines a single phase in a station's passenger spawn evolution.
/// </summary>
public class PassengerSpawnPhase
{
    /// <summary>
    /// Number of days after the station has appeared before this phase activates.
    /// For example, a value of 2 means this phase kicks in 2 days after the station spawned.
    /// </summary>
    public int AfterDays { get; set; }

    /// <summary>
    /// How often (in game hours) a new passenger spawns at this station during this phase.
    /// Lower values mean more frequent spawning (higher difficulty).
    /// </summary>
    public int FrequencyInHours { get; set; }
}

/// <summary>
/// Represents a water tile on the level grid.
/// </summary>
public class Water
{
    public int GridX { get; set; }
    public int GridY { get; set; }
}