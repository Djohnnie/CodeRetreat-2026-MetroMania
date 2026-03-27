namespace MetroMania.Engine.Model;

public abstract record PlayerAction
{
    public static PlayerAction None => new NoAction();
}

public sealed record NoAction : PlayerAction;

/// <summary>
/// Creates a new metro line from an available line resource by connecting two or more stations.
/// The player assigns a <see cref="LineId"/> to identify the line in future actions.
/// </summary>
public sealed record CreateLine(Guid LineId, IReadOnlyList<Guid> StationIds) : PlayerAction;

/// <summary>
/// Connects additional metro stations to an existing line.
/// </summary>
public sealed record ExtendLine(Guid LineId, IReadOnlyList<Guid> StationIds) : PlayerAction;

/// <summary>
/// Removes an entire metro line and makes the line resource available again.
/// </summary>
public sealed record RemoveLine(Guid LineId) : PlayerAction;

/// <summary>
/// Adds an available metro vehicle to an existing line, starting at the specified station.
/// </summary>
public sealed record AddVehicle(Guid LineId, Guid StationId) : PlayerAction;
