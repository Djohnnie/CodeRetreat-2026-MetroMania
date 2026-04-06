namespace MetroMania.Orleans.Contracts.Models;

[GenerateSerializer]
public record FrameRender
{
    [Id(0)] public int Hour { get; init; }
    [Id(1)] public string SvgContent { get; init; } = string.Empty;
    [Id(2)] public string JsonContent { get; init; } = string.Empty;
}
