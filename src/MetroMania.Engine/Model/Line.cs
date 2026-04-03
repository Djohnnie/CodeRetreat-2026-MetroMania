namespace MetroMania.Engine.Model;

public record Line
{
    public Guid LineId { get; init; }
    public IReadOnlyList<Guid> StationIds { get; init; } = [];
}