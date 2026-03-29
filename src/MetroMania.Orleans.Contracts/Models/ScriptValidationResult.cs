namespace MetroMania.Orleans.Contracts.Models;

[GenerateSerializer]
public record ScriptValidationResult
{
    [Id(0)] public bool Success { get; init; }
    [Id(1)] public List<string> Errors { get; init; } = [];
}
