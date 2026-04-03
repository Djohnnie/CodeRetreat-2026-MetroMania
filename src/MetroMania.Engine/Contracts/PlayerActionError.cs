namespace MetroMania.Engine.Contracts;

/// <summary>
/// Well-known error codes passed to <see cref="IMetroManiaRunner.OnInvalidPlayerAction"/>.
/// Group 1xx = CreateLine errors, 2xx = AddVehicleToLine errors.
/// </summary>
public static class PlayerActionError
{
    // ── CreateLine ────────────────────────────────────────────────────────────

    /// <summary>No line resource with the given LineId exists in the player's resources.</summary>
    public const int LineResourceNotFound = 100;

    /// <summary>The line resource exists but is already in use (already deployed on the map).</summary>
    public const int LineResourceAlreadyInUse = 101;

    /// <summary>FromStationId and ToStationId are the same — a line cannot loop on a single station.</summary>
    public const int LineStationsSameStation = 102;

    /// <summary>The two stations are already directly connected on an existing line.</summary>
    public const int LineSegmentAlreadyExists = 103;

    /// <summary>
    /// FromStationId is not at either terminal of the existing line and therefore
    /// cannot be used as the extension anchor.
    /// </summary>
    public const int LineExtendFromNotTerminal = 104;

    /// <summary>
    /// ToStationId already appears somewhere in the existing line — adding it again
    /// would create a duplicate stop or a loop.
    /// </summary>
    public const int LineExtendToAlreadyOnLine = 105;

    // ── AddVehicleToLine ──────────────────────────────────────────────────────

    /// <summary>No unused Train resource with the given VehicleId exists.</summary>
    public const int TrainResourceNotFound = 200;

    /// <summary>The target line does not exist on the map.</summary>
    public const int TrainLineNotFound = 201;

    /// <summary>The requested spawn station is not part of the target line.</summary>
    public const int TrainStationNotOnLine = 202;

    /// <summary>The requested spawn station has not yet appeared on the map.</summary>
    public const int TrainStationNotSpawned = 203;

    /// <summary>
    /// The line already has as many trains as it has stations (one train per stop maximum).
    /// </summary>
    public const int TrainLineAtCapacity = 204;

    /// <summary>Another train is currently occupying the requested spawn tile.</summary>
    public const int TrainTileOccupied = 205;
}
