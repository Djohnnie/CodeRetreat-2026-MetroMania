namespace MetroMania.Engine.Model;

public abstract record PlayerAction
{
    public static PlayerAction None => new NoAction();
}

public sealed record NoAction : PlayerAction;

/// <summary>
/// Creates a new metro line by consuming an available line resource and connecting two or more stations.
/// <paramref name="LineId"/> must be the Id of an available Line resource.
/// </summary>
public sealed record CreateLine(Guid LineId, IReadOnlyList<Guid> StationIds) : PlayerAction;

/// <summary>
/// Removes an entire metro line and releases the line resource and all vehicles on it.
/// <paramref name="LineId"/> must be the Id of a used Line resource.
/// </summary>
public sealed record RemoveLine(Guid LineId) : PlayerAction;

/// <summary>
/// Adds an available vehicle to an existing line, positioned at the specified station.
/// <paramref name="VehicleId"/> must be the Id of an available vehicle resource.
/// <paramref name="LineId"/> must be the Id of a used Line resource.
/// </summary>
public sealed record AddVehicleToLine(Guid VehicleId, Guid LineId, Guid StationId) : PlayerAction;

/// <summary>
/// Removes a vehicle from its line and releases the vehicle resource.
/// <paramref name="VehicleId"/> must be the Id of a used vehicle resource.
/// </summary>
public sealed record RemoveVehicle(Guid VehicleId) : PlayerAction;

/// <summary>
/// Extends an existing line by adding a station at the front or back.
/// <paramref name="FromStationId"/> must be the first or last station on the line.
/// <paramref name="ToStationId"/> is the new station to append at that end.
/// </summary>
public sealed record ExtendLine(Guid LineId, Guid FromStationId, Guid ToStationId) : PlayerAction;

/// <summary>
/// Inserts a station into an existing line between two adjacent stations.
/// <paramref name="FromStationId"/> and <paramref name="ToStationId"/> must be adjacent on the line.
/// </summary>
public sealed record InsertStationInLine(Guid LineId, Guid NewStationId, Guid FromStationId, Guid ToStationId) : PlayerAction;
