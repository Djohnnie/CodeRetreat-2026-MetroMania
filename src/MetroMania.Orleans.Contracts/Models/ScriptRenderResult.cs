namespace MetroMania.Orleans.Contracts.Models;

[GenerateSerializer]
public record ScriptRenderResult
{
    [Id(0)] public bool Success { get; init; }
    [Id(1)] public string? Error { get; init; }
    [Id(2)] public List<FrameRender> Renders { get; init; } = [];
}
