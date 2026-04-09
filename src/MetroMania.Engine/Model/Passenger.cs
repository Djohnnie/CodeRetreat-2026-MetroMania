using MetroMania.Domain.Enums;

namespace MetroMania.Engine.Model;

/// <summary>
/// Represents a passenger waiting at a station or riding on a train, heading toward a station of a specific shape type.
/// </summary>
/// <param name="DestinationType">The station shape type that this passenger wants to reach.</param>
/// <param name="SpawnedAtHour">The total elapsed hour at which this passenger was spawned.</param>
public record Passenger(StationType DestinationType, int SpawnedAtHour)
{
    /// <summary>Unique identifier for this passenger.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The station where this passenger is currently waiting, or <c>null</c> if the passenger is aboard a train.
    /// </summary>
    public Guid? StationId { get; init; }
}