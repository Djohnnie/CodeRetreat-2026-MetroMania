using MetroMania.Domain.Enums;

namespace MetroMania.Engine.Model;

/// <summary>
/// Represents a metro station placed on the game grid.
/// </summary>
public record Station
{
    /// <summary>Unique identifier for this station.</summary>
    public Guid Id { get; init; }

    /// <summary>Grid coordinates where the station is placed.</summary>
    public Location Location { get; init; }

    /// <summary>The shape type of the station, which determines which passengers it can serve.</summary>
    public StationType StationType { get; init; }
}