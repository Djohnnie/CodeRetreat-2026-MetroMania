using MetroMania.Domain.Enums;

namespace MetroMania.Engine.Model;

public record Station
{
    public Guid Id { get; init; }
    public Location Location { get; init; }
    public StationType StationType { get; init; }
}