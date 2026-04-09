using MetroMania.Domain.Enums;
using MetroMania.Engine.Contracts;
using MetroMania.Engine.Model;

namespace MetroMania.Demo.Runners;

/// <summary>
/// A general-purpose, adaptive runner that maximises score on any level layout.
///
/// Strategy — "Closest-first + Type-diverse" pattern:
///
///   The first line (backbone) connects the **closest pair** of stations,
///   preferring a pair with different station types so passengers can route
///   from the very first tick.
///
///   New stations are connected **closest-first**: the unconnected station
///   nearest to the existing backbone is always connected next (crowded
///   stations override this for safety). Extension uses the shortest-distance
///   option — terminal head/tail or mid-line insertion — whichever adds the
///   least travel distance.
///
///   When spare Line resources exist, a spoke line is created from the
///   backbone to the target. The anchor station is chosen to maximise
///   **type diversity** — preferring a backbone station whose type differs
///   from the target's — while staying as close as possible. This ensures
///   every new line bridges different station types so passengers reach
///   their destination types faster.
///
///   After all stations are on the backbone, excess lines create shortcut
///   connections scored by network-vs-direct distance ratio, type diversity,
///   and current passenger load.
///
///   Trains are deployed greedily: trainless lines first, then lines with the
///   most spare capacity, at the station with the most waiting passengers.
///
///   **Train rebalancing**: When no idle trains are available but a station is
///   building up passengers dangerously (≥7 waiting), the runner removes the
///   least-valuable train from a low-demand line with surplus trains. The freed
///   resource is then naturally redeployed by TryDeployTrain to the neediest line.
/// </summary>
internal class OptimalRunner : IMetroManiaRunner
{
    private readonly Dictionary<Guid, (Location Location, StationType Type)> _stations = new();
    private readonly List<Guid> _spawnOrder = [];
    private readonly HashSet<Guid> _crowdedStationIds = new();
    private Guid _backboneLineId;

    /// <summary>
    /// Tracks the train being removed for rebalancing. While set, no further
    /// rebalance removals are issued. Cleared once the train is fully removed.
    /// </summary>
    private Guid? _rebalanceTrainId;

    // ════════════════════════════════════════════════════════════════════════════
    // Main decision loop
    // ════════════════════════════════════════════════════════════════════════════

    public PlayerAction OnHourTicked(GameSnapshot snapshot)
    {
        return TryConnectToBackbone(snapshot)
            ?? TryDeployTrain(snapshot)
            ?? TryRebalanceTrain(snapshot)
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
        var backboneStationIds = backbone.StationIds.ToHashSet();
        var unconnected = _stations.Keys.Where(id => !backboneStationIds.Contains(id)).ToList();

        if (unconnected.Count == 0)
            return null;

        // Prioritise crowded stations (safety), then closest to backbone so
        // the network grows outward in tight, efficient segments.
        var target = unconnected
            .Where(id => _crowdedStationIds.Contains(id))
            .OrderBy(id => MinDistToLine(backbone.StationIds, id))
            .FirstOrDefault();

        target = target != default
            ? target
            : unconnected
                .OrderBy(id => MinDistToLine(backbone.StationIds, id))
                .First();

        // If a spare Line resource exists, create a spoke from the best backbone
        // anchor to the target. Prefer an anchor with a different station type
        // for better passenger routing, then pick the closest.
        var idleLine = snapshot.Resources
            .FirstOrDefault(r => r.Type == ResourceType.Line && !r.InUse);

        if (idleLine is not null)
        {
            var targetType = _stations[target].Type;
            var targetLoc = _stations[target].Location;

            var anchor = backbone.StationIds
                .Where(sid => _stations.ContainsKey(sid))
                .OrderByDescending(sid => _stations[sid].Type != targetType ? 1 : 0)
                .ThenBy(sid => Chebyshev(_stations[sid].Location, targetLoc))
                .First();

            return new CreateLine(idleLine.Id, anchor, target);
        }

        // No spare resource — extend the backbone using the shortest-distance option:
        // terminal extension (head or tail) vs mid-line insertion between consecutive stations.
        return BestBackboneExtension(backbone, target);
    }

    private PlayerAction? CreateBackbone(GameSnapshot snapshot)
    {
        if (_spawnOrder.Count < 2)
            return null;

        var lineResource = snapshot.Resources
            .FirstOrDefault(r => r.Type == ResourceType.Line && !r.InUse);
        if (lineResource is null)
            return null;

        // Pick the closest pair of stations, preferring different types so
        // passengers can route between types from the very first line.
        Guid bestA = default, bestB = default;
        int bestDist = int.MaxValue;
        bool bestDiffType = false;

        for (int i = 0; i < _spawnOrder.Count; i++)
        for (int j = i + 1; j < _spawnOrder.Count; j++)
        {
            var a = _spawnOrder[i];
            var b = _spawnOrder[j];
            int dist = Chebyshev(_stations[a].Location, _stations[b].Location);
            bool diffType = _stations[a].Type != _stations[b].Type;

            bool isBetter = (!bestDiffType && diffType)
                         || (diffType == bestDiffType && dist < bestDist);

            if (isBetter)
            {
                bestA = a;
                bestB = b;
                bestDist = dist;
                bestDiffType = diffType;
            }
        }

        _backboneLineId = lineResource.Id;
        return new CreateLine(lineResource.Id, bestA, bestB);
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
    // Priority 3 — Rebalance: pull a train from a low-demand line to help a
    //              high-demand station when no idle trains are available
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Passenger threshold at a single station that triggers a rebalance attempt.
    /// Chosen below the overrun warning (10) to give the removed train time to
    /// finish its pending-removal cycle and be redeployed.
    /// </summary>
    private const int RebalancePassengerThreshold = 7;

    private PlayerAction? TryRebalanceTrain(GameSnapshot snapshot)
    {
        // If a rebalance removal is in flight, wait for it to complete.
        if (_rebalanceTrainId.HasValue)
        {
            bool stillExists = snapshot.Trains.Any(t => t.TrainId == _rebalanceTrainId.Value);
            if (stillExists)
                return null;
            _rebalanceTrainId = null;
        }

        // Only rebalance when there are no idle trains — otherwise TryDeployTrain handles it.
        if (snapshot.Resources.Any(r => r.Type == ResourceType.Train && !r.InUse))
            return null;

        var activeLines = snapshot.Lines.Where(l => !l.PendingRemoval).ToList();
        if (activeLines.Count < 2)
            return null;

        var activeTrains = snapshot.Trains.Where(t => !t.PendingRemoval).ToList();
        if (activeTrains.Count < 2)
            return null;

        var trainsByLine = activeTrains
            .GroupBy(t => t.LineId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Check if any station on an active line is in danger.
        bool hasNeedyStation = false;
        foreach (var line in activeLines)
        {
            int lineTrainCount = trainsByLine.GetValueOrDefault(line.LineId)?.Count ?? 0;
            foreach (var sid in line.StationIds)
            {
                int waiting = snapshot.Passengers.Count(p => p.StationId == sid);

                // Trainless line with passengers waiting, or station nearing overrun.
                if ((lineTrainCount == 0 && waiting > 0) || waiting >= RebalancePassengerThreshold)
                {
                    hasNeedyStation = true;
                    break;
                }
            }
            if (hasNeedyStation) break;
        }

        if (!hasNeedyStation)
            return null;

        // Find the best donor: a line with 2+ trains and the lowest demand-per-train.
        Train? bestDonor = null;
        double bestDonorScore = double.MaxValue;

        foreach (var line in activeLines)
        {
            if (!trainsByLine.TryGetValue(line.LineId, out var lineTrains) || lineTrains.Count < 2)
                continue;

            int demand = line.StationIds
                .Sum(sid => snapshot.Passengers.Count(p => p.StationId == sid));
            double demandPerTrain = (double)demand / lineTrains.Count;

            // Pick the train with the fewest passengers on board (quickest removal).
            var candidate = lineTrains.OrderBy(t => t.Passengers.Count).First();

            // Lower score = better donor (low demand, few passengers on board).
            double score = demandPerTrain + candidate.Passengers.Count * 0.5;

            if (score < bestDonorScore)
            {
                bestDonorScore = score;
                bestDonor = candidate;
            }
        }

        if (bestDonor is null)
            return null;

        _rebalanceTrainId = bestDonor.TrainId;
        return new RemoveVehicle(bestDonor.TrainId);
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Priority 4 — Create shortcut lines when all stations are on the backbone
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

    /// <summary>
    /// Minimum Chebyshev distance from <paramref name="stationId"/> to any station on
    /// the given line. Used for closest-first connection ordering.
    /// </summary>
    private int MinDistToLine(IReadOnlyList<Guid> lineStationIds, Guid stationId)
    {
        var loc = _stations[stationId].Location;
        int min = int.MaxValue;
        foreach (var sid in lineStationIds)
        {
            if (!_stations.TryGetValue(sid, out var info)) continue;
            int d = Chebyshev(info.Location, loc);
            if (d < min) min = d;
        }
        return min;
    }

    /// <summary>
    /// Chooses the best way to add <paramref name="targetId"/> to the backbone:
    /// terminal extension (head or tail) vs mid-line insertion between consecutive
    /// stations, whichever adds the least total travel distance.
    /// </summary>
    private PlayerAction BestBackboneExtension(Line backbone, Guid targetId)
    {
        var targetLoc = _stations[targetId].Location;
        var stationIds = backbone.StationIds;

        // ── Terminal options ────────────────────────────────────────────────
        int headDist = Chebyshev(_stations[stationIds[0]].Location, targetLoc);
        int tailDist = Chebyshev(_stations[stationIds[^1]].Location, targetLoc);

        Guid bestTerminal;
        int bestTerminalCost;
        if (headDist <= tailDist)
        {
            bestTerminal = stationIds[0];
            bestTerminalCost = headDist;
        }
        else
        {
            bestTerminal = stationIds[^1];
            bestTerminalCost = tailDist;
        }

        // ── Mid-insertion options ───────────────────────────────────────────
        // For each consecutive pair A→B on the backbone, inserting the target
        // replaces the direct A→B segment with A→target→B. The detour cost is:
        //   Chebyshev(A, target) + Chebyshev(target, B) − Chebyshev(A, B)
        Guid bestInsertFrom = default, bestInsertTo = default;
        int bestDetourCost = int.MaxValue;

        for (int i = 0; i < stationIds.Count - 1; i++)
        {
            var fromId = stationIds[i];
            var toId = stationIds[i + 1];

            if (!_stations.TryGetValue(fromId, out var fromInfo)
             || !_stations.TryGetValue(toId, out var toInfo))
                continue;

            int directSegment = Chebyshev(fromInfo.Location, toInfo.Location);
            int detour = Chebyshev(fromInfo.Location, targetLoc)
                       + Chebyshev(targetLoc, toInfo.Location)
                       - directSegment;

            if (detour < bestDetourCost)
            {
                bestDetourCost = detour;
                bestInsertFrom = fromId;
                bestInsertTo = toId;
            }
        }

        // Pick whichever approach adds less distance.
        if (bestInsertFrom != default && bestDetourCost < bestTerminalCost)
            return new ExtendLineInBetween(backbone.LineId, bestInsertFrom, targetId, bestInsertTo);

        return new ExtendLineFromTerminal(backbone.LineId, bestTerminal, targetId);
    }

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
