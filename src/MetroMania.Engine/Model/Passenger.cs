using MetroMania.Domain.Enums;

namespace MetroMania.Engine.Model;

public record Passenger(StationType DestinationType, int SpawnedAtHour)
{
    public Guid Id { get; init; } = Guid.NewGuid();
}