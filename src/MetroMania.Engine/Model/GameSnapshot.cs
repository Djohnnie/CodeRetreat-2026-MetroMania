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
    public required Dictionary<Location, StationSnapshot> Stations { get; init; }
}

/// <summary>
/// The state of a single station at a point in time.
/// </summary>
public class StationSnapshot
{
    public required StationType Type { get; init; }
    public required List<Passenger> Passengers { get; init; }
}
