using MetroMania.Domain.Enums;

namespace MetroMania.Engine.Model;

/// <summary>
/// Captures the state of a running game at a specific point in time.
/// </summary>
public class GameSnapshot
{
    public required GameTime Time { get; init; }
    public required int TotalHoursElapsed { get; init; }
    public required bool GameOver { get; init; }

    /// <summary>
    /// The player's total score, based on the number of passengers
    /// successfully transported to their destination.
    /// </summary>
    public required int TotalScore { get; init; }

    public required Dictionary<Location, StationSnapshot> Stations { get; init; }

    /// <summary>
    /// All resources the player has received, including both available and in-use resources.
    /// </summary>
    public required IReadOnlyList<ResourceSnapshot> Resources { get; init; }
}

/// <summary>
/// The state of a single station at a point in time.
/// </summary>
public class StationSnapshot
{
    public required Guid Id { get; init; }
    public required StationType Type { get; init; }
    public required List<Passenger> Passengers { get; init; }
}

/// <summary>
/// The state of a player resource (line or vehicle) at a point in time.
/// </summary>
public record ResourceSnapshot(Guid Id, ResourceType Type, bool InUse);
