using MetroMania.Domain.Enums;

namespace MetroMania.Engine.Model;

public record Resource
{
    public ResourceType Type { get; init; }
    public bool InUse { get; init; }
}