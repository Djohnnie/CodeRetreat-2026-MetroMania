namespace MetroMania.Engine.Model;

/// <summary>
/// Extends <see cref="SimulationResult"/> with execution metadata returned after running a player script against a level.
/// </summary>
public record GameResult : SimulationResult
{
    /// <summary>The wall-clock time taken to execute the simulation.</summary>
    public TimeSpan ProcessingTime { get; init; }
}