namespace MetroMania.Engine.Model;

/// <summary>
/// Contains the outcome metrics of a completed game simulation.
/// </summary>
public record SimulationResult
{
    /// <summary>The total score achieved by the player across the entire simulation.</summary>
    public int TotalScore { get; init; }

    /// <summary>The number of in-game days the player survived before game over (or the simulation ended).</summary>
    public int DaysSurvived { get; init; }

    /// <summary>The total number of passengers that spawned during the simulation.</summary>
    public int TotalPassengersSpawned { get; init; }

    /// <summary>The total number of valid player actions executed during the simulation.</summary>
    public int NumberOfPlayerActions { get; init; }

    /// <summary>A chronological list of game state snapshots captured during the simulation, used for replay and rendering.</summary>
    public IReadOnlyList<GameSnapshot> GameSnapshots { get; init; } = [];
}