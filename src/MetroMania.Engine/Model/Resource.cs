using MetroMania.Domain.Enums;

namespace MetroMania.Engine.Model;

/// <summary>
/// Represents a deployable resource (line or train) available to the player.
/// </summary>
public record Resource
{
    /// <summary>Unique identifier for this resource.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The type of resource (Line or Train).</summary>
    public ResourceType Type { get; init; }

    /// <summary>Whether this resource is currently deployed on the map.</summary>
    public bool InUse { get; init; }
}