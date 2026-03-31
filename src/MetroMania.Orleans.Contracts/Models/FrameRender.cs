namespace MetroMania.Orleans.Contracts.Models;

[GenerateSerializer]
public record FrameRender
{
    [Id(0)] public int Day { get; init; }
    [Id(1)] public string SvgContent { get; init; } = string.Empty;
}
