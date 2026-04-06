using MetroMania.Domain.Enums;
using MetroMania.Engine.Contracts;
using MetroMania.Engine.Model;

namespace MetroMania.Demo.Runners;

/// <summary>
/// A general-purpose, adaptive runner that maximises score on any level layout.
///
/// Strategy — "Backbone + Hub" pattern:
///
///   The first line created becomes the **backbone**. Its head station (index 0)
///   acts as a hub. Every new station is connected in two steps:
///
///     1. If a spare Line resource is available, create a NEW line from the
///        backbone head → new station. This gives the station a direct connection
///        to the hub and creates an interchange when the backbone is later extended
///        to include it.
///
///     2. When no spare Line resource exists, extend the backbone from its tail
///        (index ^1) → new station. This adds the station to the backbone and —
///        if step 1 already ran — creates a dual-line interchange.
///
///   The result is a hub-and-spoke topology where every station sits on the
///   backbone AND (often) has a secondary spoke line back to the hub.
///
///   After all stations are connected to the backbone, excess Line resources
///   (keeping 1 in reserve for future stations) are used to create shortcut
///   lines between stations whose network distance significantly exceeds their
///   direct distance.
///
///   Trains are deployed greedily: trainless lines first, then lines with the
///   most spare capacity, at the station with the most waiting passengers.
/// </summary>
internal class OptimalRunner : IMetroManiaRunner
{
    private readonly Dictionary<Guid, (Location Location, StationType Type)> _stations = new();
    private readonly List<Guid> _spawnOrder = [];
    private readonly HashSet<Guid> _crowdedStationIds = new();
    private Guid _backboneLineId;

    // ════════════════════════════════════════════════════════════════════════════
    // Main decision loop
    // ════════════════════════════════════════════════════════════════════════════

    public PlayerAction OnHourTicked(GameSnapshot snapshot)
    {
        return TryConnectToBackbone(snapshot)
            ?? TryDeployTrain(snapshot)
            ?? TryCreateShortcutLine(snapshot)
            ?? PlayerAction.None;
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Priority 1 — Connect stations to the backbone
    // ════════════════════════════════════════════════════════════════════════════

    private PlayerAction? TryConnectToBackbone(GameSnapshot snapshot)
    {
        var activeLines = snapshot.Lines.Where(l => !l.PendingRemoval).ToList();

        // Create the backbone between the closest pair of stations.
        if (activeLines.Count == 0)
            return CreateBackbone(snapshot);

        // Find the backbone line (may have been set in a prior tick).
        var backbone = activeLines.FirstOrDefault(l => l.LineId == _backboneLineId)
                    ?? activeLines[0];
        _backboneLineId = backbone.LineId;

        // Connectivity is checked against the BACKBONE only — a station on a
        // secondary spoke line is still "unconnected" until the backbone reaches it.
        // This is the key to creating dual-line interchanges.
        var backboneStationIds = backbone.StationIds.ToHashSet();
        var unconnected = _stations.Keys.Where(id => !backboneStationIds.Contains(id)).ToList();

        if (unconnected.Count == 0)
            return null;

        // Prioritise crowded stations (safety), then use spawn order to match the
        // natural station progression — this avoids zigzag backbones.
        var target = unconnected
            .Where(id => _crowdedStationIds.Contains(id))
            .FirstOrDefault();

        target = target != default
            ? target
            : unconnected.OrderBy(id => _spawnOrder.IndexOf(id)).First();

        // If a spare Line resource exists, create a spoke: backbone HEAD → target.
        var idleLine = snapshot.Resources
            .FirstOrDefault(r => r.Type == ResourceType.Line && !r.InUse);

        if (idleLine is not null)
            return new CreateLine(idleLine.Id, backbone.StationIds[0], target);

        // No spare resource — extend the backbone from its TAIL → target.
        return new CreateLine(backbone.LineId, backbone.StationIds[^1], target);
    }

    private PlayerAction? CreateBackbone(GameSnapshot snapshot)
    {
        if (_spawnOrder.Count < 2)
            return null;

        var lineResource = snapshot.Resources
            .FirstOrDefault(r => r.Type == ResourceType.Line && !r.InUse);
        if (lineResource is null)
            return null;

        // Use the first two spawned stations — this produces natural, linear
        // backbones that grow well as new stations appear.
        _backboneLineId = lineResource.Id;
        return new CreateLine(lineResource.Id, _spawnOrder[0], _spawnOrder[1]);
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Priority 2 — Deploy idle trains
    // ════════════════════════════════════════════════════════════════════════════

    private PlayerAction? TryDeployTrain(GameSnapshot snapshot)
    {
        var idleTrain = snapshot.Resources
            .FirstOrDefault(r => r.Type == ResourceType.Train && !r.InUse);
        if (idleTrain is null)
            return null;

        var activeLines = snapshot.Lines.Where(l => !l.PendingRemoval).ToList();
        if (activeLines.Count == 0)
            return null;

        var trainsByLine = snapshot.Trains
            .Where(t => !t.PendingRemoval)
            .GroupBy(t => t.LineId)
            .ToDictionary(g => g.Key, g => g.Count());

        // Trainless lines first, then the line with the largest capacity gap.
        var candidate = activeLines
            .Select(l => (
                Line: l,
                Trains: trainsByLine.GetValueOrDefault(l.LineId, 0),
                Capacity: l.StationIds.Count))
            .Where(x => x.Trains < x.Capacity)
            .OrderByDescending(x => x.Trains == 0 ? 1 : 0)
            .ThenByDescending(x => x.Capacity - x.Trains)
            .ThenByDescending(x => x.Capacity)
            .Select(x => x.Line)
            .FirstOrDefault();

        if (candidate is null)
            return null;

        // Deploy at the unoccupied station with the most waiting passengers.
        var occupiedTiles = snapshot.Trains.Select(t => t.TilePosition).ToHashSet();

        var deployStation = candidate.StationIds
            .Where(sid => _stations.TryGetValue(sid, out var info)
                       && !occupiedTiles.Contains(info.Location))
            .OrderByDescending(sid => snapshot.Passengers.Count(p => p.StationId == sid))
            .FirstOrDefault();

        if (deployStation == default)
            return null;

        return new AddVehicleToLine(idleTrain.Id, candidate.LineId, deployStation);
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Priority 3 — Create shortcut lines when all stations are on the backbone
    // ════════════════════════════════════════════════════════════════════════════

    private PlayerAction? TryCreateShortcutLine(GameSnapshot snapshot)
    {
        // Keep at least 1 Line resource in reserve for future station connections.
        var idleLines = snapshot.Resources
            .Where(r => r.Type == ResourceType.Line && !r.InUse)
            .ToList();
        if (idleLines.Count < 2)
            return null;

        var idleLine = idleLines[0];

        var activeLines = snapshot.Lines.Where(l => !l.PendingRemoval).ToList();
        if (activeLines.Count == 0)
            return null;

        var stations = snapshot.Stations.Values.ToList();
        if (stations.Count < 3)
            return null;

        bool hasIdleTrain = snapshot.Resources
            .Any(r => r.Type == ResourceType.Train && !r.InUse);

        // If every line is at train capacity and we have idle trains,
        // we must create a new line so the excess train can be deployed.
        bool allLinesFull = activeLines.All(l =>
        {
            int trains = snapshot.Trains.Count(t => t.LineId == l.LineId && !t.PendingRemoval);
            return trains >= l.StationIds.Count;
        });
        bool mustCreate = hasIdleTrain && allLinesFull;

        double bestScore = 0;
        Guid bestFrom = default, bestTo = default;

        for (int i = 0; i < stations.Count; i++)
        for (int j = i + 1; j < stations.Count; j++)
        {
            var a = stations[i];
            var b = stations[j];

            if (AlreadyDirectlyConnected(activeLines, a.Id, b.Id)) continue;

            int directDist = Chebyshev(a.Location, b.Location);
            if (directDist < 2) continue;

            int networkDist = NetworkDistance(snapshot, activeLines, a.Id, b.Id);

            double shortcutFactor = networkDist == int.MaxValue
                ? 3.0
                : (double)networkDist / directDist;

            if (!mustCreate && shortcutFactor < 1.5) continue;

            int waitingLoad = snapshot.Passengers.Count(p => p.StationId == a.Id)
                            + snapshot.Passengers.Count(p => p.StationId == b.Id);

            double score = shortcutFactor
                         * (a.StationType != b.StationType ? 2.0 : 1.0)
                         * (1.0 + waitingLoad * 0.1);

            if (!hasIdleTrain)
                score *= 0.3;

            if (score > bestScore)
            {
                bestScore = score;
                bestFrom = a.Id;
                bestTo = b.Id;
            }
        }

        if (bestFrom == default)
            return null;

        return new CreateLine(idleLine.Id, bestFrom, bestTo);
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Helpers
    // ════════════════════════════════════════════════════════════════════════════

    private static int Chebyshev(Location a, Location b)
        => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

    private static bool AlreadyDirectlyConnected(List<Line> lines, Guid a, Guid b)
        => lines.Any(l =>
            l.StationIds.Count == 2
            && l.StationIds.Contains(a)
            && l.StationIds.Contains(b));

    /// <summary>
    /// Dijkstra shortest-path between two stations over the current line network.
    /// </summary>
    private static int NetworkDistance(
        GameSnapshot snapshot, List<Line> lines, Guid from, Guid to)
    {
        var adj = new Dictionary<Guid, List<(Guid Neighbor, int Cost)>>();
        foreach (var s in snapshot.Stations.Values)
            adj[s.Id] = [];

        foreach (var line in lines)
        {
            var ids = line.StationIds;
            for (int i = 0; i < ids.Count - 1; i++)
            {
                var locA = snapshot.Stations.FirstOrDefault(kvp => kvp.Value.Id == ids[i]).Key;
                var locB = snapshot.Stations.FirstOrDefault(kvp => kvp.Value.Id == ids[i + 1]).Key;
                if (locA == default || locB == default) continue;

                int cost = Chebyshev(locA, locB);
                if (adj.ContainsKey(ids[i])) adj[ids[i]].Add((ids[i + 1], cost));
                if (adj.ContainsKey(ids[i + 1])) adj[ids[i + 1]].Add((ids[i], cost));
            }
        }

        var dist = new Dictionary<Guid, int> { [from] = 0 };
        var pq = new PriorityQueue<Guid, int>();
        pq.Enqueue(from, 0);

        while (pq.TryDequeue(out var cur, out int d))
        {
            if (d > dist.GetValueOrDefault(cur, int.MaxValue)) continue;
            if (cur == to) return d;
            if (!adj.TryGetValue(cur, out var neighbors)) continue;

            foreach (var (nb, cost) in neighbors)
            {
                int nd = d + cost;
                if (nd < dist.GetValueOrDefault(nb, int.MaxValue))
                {
                    dist[nb] = nd;
                    pq.Enqueue(nb, nd);
                }
            }
        }

        return int.MaxValue;
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Event callbacks
    // ════════════════════════════════════════════════════════════════════════════

    public void OnStationSpawned(GameSnapshot snapshot, Guid stationId, Location location, StationType stationType)
    {
        _stations[stationId] = (location, stationType);
        _spawnOrder.Add(stationId);
    }

    public void OnStationCrowded(GameSnapshot snapshot, Guid stationId, int numberOfPassengersWaiting)
        => _crowdedStationIds.Add(stationId);

    public void OnDayStart(GameSnapshot snapshot) { }
    public void OnWeeklyGiftReceived(GameSnapshot snapshot, ResourceType gift) { }
    public void OnPassengerSpawned(GameSnapshot snapshot, Guid stationId, Guid passengerId) { }
    public void OnGameOver(GameSnapshot snapshot, Guid stationId) { }
    public void OnInvalidPlayerAction(GameSnapshot snapshot, int code, string description) { }
    public void OnVehicleRemoved(GameSnapshot snapshot, Guid vehicleId) { }
    public void OnLineRemoved(GameSnapshot snapshot, Guid lineId) { }
}
