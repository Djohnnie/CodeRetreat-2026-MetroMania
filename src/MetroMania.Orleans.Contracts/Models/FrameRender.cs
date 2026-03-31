namespace MetroMania.Orleans.Contracts.Models;

[GenerateSerializer]
public record FrameRender
{
    [Id(0)] public int Hour { get; init; }
    [Id(1)] public string SvgContent { get; init; } = string.Empty;
}
