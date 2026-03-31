using MetroMania.Domain.Enums;

namespace MetroMania.Engine.Model;

public record Passenger(StationType DestinationType, int SpawnedAtHour);
