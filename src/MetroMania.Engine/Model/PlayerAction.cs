namespace MetroMania.Engine.Model;

public abstract record PlayerAction
{
    public static PlayerAction None => new NoAction();
}

/// <summary>
/// Represents a player action that performs no operation during the current game tick.
/// </summary>
/// <remarks>
/// Use this action when the player bot does not wish to make
/// any changes to the game state for the current tick.
/// </remarks>
public sealed record NoAction : PlayerAction;

/// <summary>
/// Creates a new metro line by consuming an available line resource and connecting two stations.
/// <paramref name="LineId"/> must be the Id of an available (not in-use) Line resource.
/// <paramref name="FromStationId"/> and <paramref name="ToStationId"/> must be the Ids of two distinct stations.
/// </summary>
public sealed record CreateLine(Guid LineId, Guid FromStationId, Guid ToStationId) : PlayerAction;

/// <summary>
/// Extends an existing line from one of its terminal stations to a new station.
/// <paramref name="LineId"/> must be the Id of an in-use Line resource (an existing line).
/// <paramref name="TerminalStationId"/> must be the first or last station on the line.
/// <paramref name="ToStationId"/> must not already appear on the line.
/// </summary>
public sealed record ExtendLineFromTerminal(Guid LineId, Guid TerminalStationId, Guid ToStationId) : PlayerAction;

/// <summary>
/// Removes an entire metro line and releases the line resource and all vehicles on it.
/// <paramref name="LineId"/> must be the Id of a used Line resource.
/// </summary>
public sealed record RemoveLine(Guid LineId) : PlayerAction;

/// <summary>
/// Adds an available vehicle to an existing line, positioned at the specified station.
/// <paramref name="VehicleId"/> must be the Id of an available vehicle resource.
/// <paramref name="LineId"/> must be the Id of a used Line resource.
/// <paramref name="StationId"/> must be the Id of a station that is already connected to the line and does not already have a vehicle on it.
/// </summary>
public sealed record AddVehicleToLine(Guid VehicleId, Guid LineId, Guid StationId) : PlayerAction;

/// <summary>
/// Removes a vehicle from its line and releases the vehicle resource.
/// <paramref name="VehicleId"/> must be the Id of a used vehicle resource.
/// </summary>
public sealed record RemoveVehicle(Guid VehicleId) : PlayerAction;