namespace MetroMania.Engine.Model;

public record GameResult : SimulationResult
{
    public TimeSpan ProcessingTime { get; init; }
}