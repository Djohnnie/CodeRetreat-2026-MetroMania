namespace MetroMania.Engine.Model;

public record GameResult
{
    public int Score { get; init; }
    public TimeSpan TimeTaken { get; init; }
    public int DaysSurvived { get; init; }
    public int TotalPassengersSpawned { get; init; }
}
