namespace MetroMania.Engine.Model;

/// <summary>
/// An immutable snapshot of the entire game state at a specific point in time, provided to the player bot on every callback.
/// </summary>
public record GameSnapshot
{
    /// <summary>The current in-game time (day, hour, and day of week).</summary>
    public required GameTime Time { get; init; }

    /// <summary>The total number of simulation hours that have elapsed since the start of the game.</summary>
    public required int TotalHoursElapsed { get; init; }

    /// <summary>The player's current cumulative score.</summary>
    public required int Score { get; init; }

    /// <summary>All resources (lines and trains) the player owns, both available and deployed.</summary>
    public required IReadOnlyList<Resource> Resources { get; init; }

    /// <summary>All stations currently on the map, keyed by their grid location.</summary>
    public required Dictionary<Location, Station> Stations { get; init; }

    /// <summary>All active metro lines on the map.</summary>
    public required IReadOnlyList<Line> Lines { get; init; }

    /// <summary>All active trains on the map, including those with pending removal.</summary>
    public required IReadOnlyList<Train> Trains { get; init; }

    /// <summary>All passengers currently in the game, whether waiting at a station or riding a train.</summary>
    public required IReadOnlyList<Passenger> Passengers { get; init; }

    /// <summary>The order identifier that will be assigned to the next line created.</summary>
    public int NextLineOrderId { get; init; }

    /// <summary>The last player action that was executed, or <c>null</c> if no action has been taken yet.</summary>
    public PlayerAction? LastAction { get; init; }
}