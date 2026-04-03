namespace MetroMania.Engine.Model;

public record GameSnapshot
{
    public required GameTime Time { get; init; }
    public required int TotalHoursElapsed { get; init; }
    public required int Score { get; init; }

    public required IReadOnlyList<Resource> Resources { get; init; }
    public required Dictionary<Location, Station> Stations { get; init; }
    public required IReadOnlyList<Line> Lines { get; init; }
    public required IReadOnlyList<Train> Trains { get; init; }
    public required IReadOnlyList<Passenger> Passengers { get; init; }

    public PlayerAction? LastAction { get; init; }
}