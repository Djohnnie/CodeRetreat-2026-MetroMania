using MetroMania.Domain.Enums;

namespace MetroMania.Engine.Model;

public record Resource
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public ResourceType Type { get; init; }
    public bool InUse { get; init; }
}