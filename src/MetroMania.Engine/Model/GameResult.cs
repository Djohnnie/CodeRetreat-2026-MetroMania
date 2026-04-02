using MetroMania.Domain.Entities;

namespace MetroMania.Engine.Model;

public record GameResult
{
    public int Score { get; init; }
    public TimeSpan TimeTaken { get; init; }
    public int DaysSurvived { get; init; }
    public int TotalPassengersSpawned { get; init; }

    /// <summary>
    /// Full debug information produced by <see cref="MetroManiaEngine.Run"/>.
    /// Contains the original level configuration and a snapshot for every simulated hour.
    /// </summary>
    public GameDebugInfo? DebugInfo { get; init; }
}

/// <summary>
/// Debug information captured during a full simulation run.
/// </summary>
public record GameDebugInfo(
    LevelData LevelData,
    IReadOnlyList<GameSnapshot> HourlySnapshots);
