using MetroMania.Domain.Enums;

namespace MetroMania.Engine.Model;

/// <summary>
/// Captures the state of a running game at a specific point in time.
/// </summary>
public class GameSnapshot
{
    public required GameTime Time { get; init; }
    public required int TotalHoursElapsed { get; init; }
    public required bool GameOver { get; init; }

    /// <summary>
    /// The player's total score, based on the number of passengers
    /// successfully transported to their destination.
    /// </summary>
    public required int TotalScore { get; init; }

    public required Dictionary<Location, StationSnapshot> Stations { get; init; }

    /// <summary>
    /// All resources the player has received, including both available and in-use resources.
    /// </summary>
    public required IReadOnlyList<ResourceSnapshot> Resources { get; init; }

    /// <summary>
    /// All active metro lines the player has created.
    /// </summary>
    public required IReadOnlyList<LineSnapshot> Lines { get; init; }

    /// <summary>
    /// All active vehicles placed on lines.
    /// </summary>
    public required IReadOnlyList<VehicleSnapshot> Vehicles { get; init; }

    /// <summary>
    /// Stations that are not part of any metro line.
    /// </summary>
    public IReadOnlyList<StationSnapshot> UnconnectedStations
    {
        get
        {
            var connectedIds = new HashSet<Guid>(Lines.SelectMany(l => l.StationIds));
            return Stations.Values.Where(s => !connectedIds.Contains(s.Id)).ToList();
        }
    }

    /// <summary>
    /// Stations that are part of at least one metro line.
    /// </summary>
    public IReadOnlyList<StationSnapshot> ConnectedStations
    {
        get
        {
            var connectedIds = new HashSet<Guid>(Lines.SelectMany(l => l.StationIds));
            return Stations.Values.Where(s => connectedIds.Contains(s.Id)).ToList();
        }
    }

    public IReadOnlyList<ResourceSnapshot> AvailableResources => Resources.Where(r => !r.InUse).ToList();
    public IReadOnlyList<ResourceSnapshot> UsedResources => Resources.Where(r => r.InUse).ToList();
    public IReadOnlyList<ResourceSnapshot> AvailableLines => Resources.Where(r => !r.InUse && r.Type == ResourceType.Line).ToList();
    public IReadOnlyList<ResourceSnapshot> UsedLines => Resources.Where(r => r.InUse && r.Type == ResourceType.Line).ToList();
    public IReadOnlyList<ResourceSnapshot> AvailableVehicles => Resources.Where(r => !r.InUse && r.Type is ResourceType.Train or ResourceType.Wagon).ToList();
    public IReadOnlyList<ResourceSnapshot> UsedVehicles => Resources.Where(r => r.InUse && r.Type is ResourceType.Train or ResourceType.Wagon).ToList();
}

/// <summary>
/// The state of a single station at a point in time.
/// </summary>
public class StationSnapshot
{
    public required Guid Id { get; init; }
    public required StationType Type { get; init; }
    public required List<Passenger> Passengers { get; init; }

    internal GameSnapshot? Snapshot { get; set; }

    /// <summary>
    /// The metro lines that pass through this station.
    /// </summary>
    public IReadOnlyList<LineSnapshot> Lines =>
        Snapshot?.Lines.Where(l => l.StationIds.Contains(Id)).ToList()
        ?? [];
}

/// <summary>
/// The state of a player resource (line or vehicle) at a point in time.
/// </summary>
public record ResourceSnapshot(Guid Id, ResourceType Type, bool InUse);

/// <summary>
/// An active metro line with its ordered station route.
/// </summary>
public class LineSnapshot
{
    public required Guid LineId { get; init; }
    public required IReadOnlyList<Guid> StationIds { get; init; }

    internal GameSnapshot? Snapshot { get; set; }

    /// <summary>
    /// The station snapshots on this line, in route order.
    /// </summary>
    public IReadOnlyList<StationSnapshot> Stations =>
        Snapshot is null
            ? []
            : StationIds
                .Select(id => Snapshot.Stations.Values.FirstOrDefault(s => s.Id == id))
                .OfType<StationSnapshot>()
                .ToList();

    /// <summary>
    /// The vehicles currently traveling on this line.
    /// </summary>
    public IReadOnlyList<VehicleSnapshot> Vehicles =>
        Snapshot?.Vehicles.Where(v => v.LineId == LineId).ToList()
        ?? [];
}

/// <summary>
/// A vehicle on a line with its current position and direction.
/// SegmentIndex identifies the segment (0 = between station[0] and station[1]).
/// Progress is 0.0 at station[SegmentIndex] and 1.0 at station[SegmentIndex+1].
/// Direction is +1 (forward) or -1 (backward). StationId is set when the vehicle
/// is exactly at a station, null when mid-segment.
/// </summary>
public class VehicleSnapshot
{
    public required Guid VehicleId { get; init; }
    public required Guid LineId { get; init; }
    public required int SegmentIndex { get; init; }
    public required float Progress { get; init; }
    public required int Direction { get; init; }
    public required Guid? StationId { get; init; }

    internal GameSnapshot? Snapshot { get; set; }

    /// <summary>
    /// The line this vehicle is traveling on.
    /// </summary>
    public LineSnapshot? Line =>
        Snapshot?.Lines.FirstOrDefault(l => l.LineId == LineId);
}
