namespace MetroMania.Orleans.Contracts.Models;

[GenerateSerializer]
public record ScriptPrepareResult
{
    [Id(0)] public bool Success { get; init; }
    [Id(1)] public string? Error { get; init; }
    [Id(2)] public int TotalFrames { get; init; }
}
