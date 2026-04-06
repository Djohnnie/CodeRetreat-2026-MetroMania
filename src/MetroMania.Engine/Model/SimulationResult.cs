namespace MetroMania.Engine.Model;

public record SimulationResult
{
    public int TotalScore { get; init; }
    public int DaysSurvived { get; init; }
    public int TotalPassengersSpawned { get; init; }
    public int NumberOfPlayerActions { get; init; }

    public IReadOnlyList<GameSnapshot> GameSnapshots { get; init; } = [];
}