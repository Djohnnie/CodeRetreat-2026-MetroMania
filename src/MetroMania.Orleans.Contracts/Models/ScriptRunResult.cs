namespace MetroMania.Orleans.Contracts.Models;

[GenerateSerializer]
public record ScriptRunResult
{
    [Id(0)] public bool Success { get; init; }
    [Id(1)] public string? Error { get; init; }
    [Id(2)] public int Score { get; init; }
    [Id(3)] public double TimeTakenMs { get; init; }
    [Id(4)] public int DaysSurvived { get; init; }
    [Id(5)] public int TotalPassengersSpawned { get; init; }

    /// <summary>
    /// JSON-serialized <see cref="MetroMania.Engine.Model.GameDebugInfo"/> containing
    /// the original level data and a snapshot for every simulated hour.
    /// </summary>
    [Id(6)] public string? DebugJson { get; init; }
}
