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

    /// <summary>
    /// Maximum number of passengers a single vehicle (train or wagon) can carry.
    /// Applies equally to both trains and wagons. Default is 6.
    /// </summary>
    public int VehicleCapacity { get; set; } = 6;

    public List<MetroStation> Stations { get; set; } = [];
    public List<Water> WaterTiles { get; set; } = [];

    /// <summary>
    /// Optional static overrides for weekly gift resource types.
    /// When a week number has an entry here, the engine uses the specified resource type
    /// instead of picking one randomly. Weeks without an entry still use the seeded RNG.
    /// </summary>
    public List<WeeklyGiftOverride> WeeklyGiftOverrides { get; set; } = [];
}

/// <summary>
/// Represents a station placed on a level grid.
/// </summary>
public class MetroStation
{
    public int GridX { get; set; }
    public int GridY { get; set; }
    public StationType StationType { get; set; }

    /// <summary>
    /// Number of days to delay before this station spawns.
    /// 0 means the station spawns immediately on day 1.
    /// 1 means the station spawns on the second day (day 2) at midnight.
    /// N means the station spawns on day N+1 at midnight.
    /// </summary>
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

/// <summary>
/// Overrides the random weekly gift for a specific week with a fixed resource type.
/// </summary>
public class WeeklyGiftOverride
{
    /// <summary>
    /// The 1-based week number. Week 1 is the first Monday (day 1), week 2 is day 8, etc.
    /// </summary>
    public int Week { get; set; }

    /// <summary>
    /// The resource type to gift on this week instead of a random one.
    /// </summary>
    public ResourceType ResourceType { get; set; }
}