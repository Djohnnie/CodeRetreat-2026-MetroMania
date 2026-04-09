using MetroMania.Domain.Enums;
using MetroMania.Engine.Contracts;
using MetroMania.Engine.Model;

namespace MetroMania.Demo.Runners;

/// <summary>
/// UltraRunner — multi-line strategy with aggressive train rebalancing.
///
/// Rules:
/// 1. Immediately connect new stations to the closest connected station.
/// 2. Immediately deploy available trains to the most crowded station.
/// 3. Cap lines at 6 stations when spare line resources exist.
/// 4. When no trains are available, steal the least busy train from the
///    line with the fewest waiting passengers (keeping ≥1 train per line).
/// 5. Always keep at least one train on every line.
/// </summary>
internal class UltraRunner : IMetroManiaRunner
{
    private readonly Dictionary<Guid, (Location Location, StationType Type)> _stations = new();
    private readonly Queue<Guid> _pendingConnections = new();
    private Guid? _pendingTrainRemoval;

    public PlayerAction OnHourTicked(GameSnapshot snapshot)
    {
        return TryConnectPendingStation(snapshot)
            ?? TryDeployTrain(snapshot)
            ?? TryStealTrain(snapshot)
            ?? PlayerAction.None;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Station connection — connect each new station to the closest one on a line
    // ═══════════════════════════════════════════════════════════════════════

    private PlayerAction? TryConnectPendingStation(GameSnapshot snapshot)
    {
        while (_pendingConnections.Count > 0)
        {
            var stationId = _pendingConnections.Peek();
            var activeLines = snapshot.Lines.Where(l => !l.PendingRemoval).ToList();

            if (activeLines.Any(l => l.StationIds.Contains(stationId)))
            {
                _pendingConnections.Dequeue();
                continue;
            }

            if (!_stations.ContainsKey(stationId))
            {
                _pendingConnections.Dequeue();
                continue;
            }

            var connectedIds = activeLines
                .SelectMany(l => l.StationIds)
                .Distinct()
                .Where(id => _stations.ContainsKey(id))
                .ToHashSet();

            if (connectedIds.Count == 0)
                return TryCreateFirstLine(snapshot);

            var targetLoc = _stations[stationId].Location;

            // Among equidistant stations, prefer ones on shorter lines (better balance)
            var lineLengths = activeLines.SelectMany(l => l.StationIds.Select(s => (StationId: s, Count: l.StationIds.Count)))
                .GroupBy(x => x.StationId)
                .ToDictionary(g => g.Key, g => g.Min(x => x.Count));

            var closestId = connectedIds
                .OrderBy(id => Chebyshev(_stations[id].Location, targetLoc))
                .ThenBy(id => lineLengths.GetValueOrDefault(id, int.MaxValue))
                .First();

            bool hasSpareLineResource = snapshot.Resources
                .Any(r => r.Type == ResourceType.Line && !r.InUse);

            var linesWithClosest = activeLines
                .Where(l => l.StationIds.Contains(closestId))
                .OrderBy(l => l.StationIds.Count)
                .ToList();

            // Rule 3: spare line + all lines with closest have ≥6 stations → new line
            if (hasSpareLineResource && linesWithClosest.All(l => l.StationIds.Count >= 6))
            {
                var res = snapshot.Resources.First(r => r.Type == ResourceType.Line && !r.InUse);
                _pendingConnections.Dequeue();
                return new CreateLine(res.Id, closestId, stationId);
            }

            // Evaluate ALL candidate actions (terminal extend + in-between insert)
            // adjacent to closest station, pick the one with least route cost.
            PlayerAction? bestAction = null;
            int bestCost = int.MaxValue;

            foreach (var line in linesWithClosest)
            {
                if (hasSpareLineResource && line.StationIds.Count >= 6)
                    continue;

                int idx = IndexOf(line.StationIds, closestId);
                if (idx < 0) continue;

                var closestLoc = _stations[closestId].Location;

                // Terminal extension (if closest is first or last)
                if (idx == 0 || idx == line.StationIds.Count - 1)
                {
                    int cost = Chebyshev(closestLoc, targetLoc);
                    if (cost < bestCost)
                    {
                        bestCost = cost;
                        bestAction = new ExtendLineFromTerminal(line.LineId, closestId, stationId);
                    }
                }

                // In-between: insert between predecessor and closest
                if (idx > 0 && _stations.TryGetValue(line.StationIds[idx - 1], out var prevInfo))
                {
                    int cost = Chebyshev(prevInfo.Location, targetLoc)
                             + Chebyshev(targetLoc, closestLoc)
                             - Chebyshev(prevInfo.Location, closestLoc);
                    if (cost < bestCost)
                    {
                        bestCost = cost;
                        bestAction = new ExtendLineInBetween(
                            line.LineId, line.StationIds[idx - 1], stationId, closestId);
                    }
                }

                // In-between: insert between closest and successor
                if (idx < line.StationIds.Count - 1
                    && _stations.TryGetValue(line.StationIds[idx + 1], out var nextInfo))
                {
                    int cost = Chebyshev(closestLoc, targetLoc)
                             + Chebyshev(targetLoc, nextInfo.Location)
                             - Chebyshev(closestLoc, nextInfo.Location);
                    if (cost < bestCost)
                    {
                        bestCost = cost;
                        bestAction = new ExtendLineInBetween(
                            line.LineId, closestId, stationId, line.StationIds[idx + 1]);
                    }
                }
            }

            if (bestAction is not null)
            {
                _pendingConnections.Dequeue();
                return bestAction;
            }

            // Fallback: extend from nearest terminal of any line (ignore 6-cap)
            var bestTerminal = activeLines
                .SelectMany(l => new[]
                {
                    (l.LineId, Terminal: l.StationIds[0]),
                    (l.LineId, Terminal: l.StationIds[^1])
                })
                .Where(x => _stations.ContainsKey(x.Terminal))
                .OrderBy(x => Chebyshev(_stations[x.Terminal].Location, targetLoc))
                .FirstOrDefault();

            if (bestTerminal.Terminal != default)
            {
                _pendingConnections.Dequeue();
                return new ExtendLineFromTerminal(bestTerminal.LineId, bestTerminal.Terminal, stationId);
            }

            break;
        }

        return null;
    }

    private static int IndexOf(IReadOnlyList<Guid> list, Guid value)
    {
        for (int i = 0; i < list.Count; i++)
            if (list[i] == value) return i;
        return -1;
    }

    private PlayerAction? TryCreateFirstLine(GameSnapshot snapshot)
    {
        if (_stations.Count < 2) return null;
        var res = snapshot.Resources.FirstOrDefault(r => r.Type == ResourceType.Line && !r.InUse);
        if (res is null) return null;

        Guid bestA = default, bestB = default;
        int bestDist = int.MaxValue;
        bool bestDiff = false;

        var ids = _stations.Keys.ToList();
        for (int i = 0; i < ids.Count; i++)
        for (int j = i + 1; j < ids.Count; j++)
        {
            int d = Chebyshev(_stations[ids[i]].Location, _stations[ids[j]].Location);
            bool diff = _stations[ids[i]].Type != _stations[ids[j]].Type;
            if ((!bestDiff && diff) || (diff == bestDiff && d < bestDist))
            {
                bestA = ids[i]; bestB = ids[j]; bestDist = d; bestDiff = diff;
            }
        }

        return new CreateLine(res.Id, bestA, bestB);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Train deployment — idle trains go to the most crowded station
    // ═══════════════════════════════════════════════════════════════════════

    private PlayerAction? TryDeployTrain(GameSnapshot snapshot)
    {
        var idle = snapshot.Resources.FirstOrDefault(r => r.Type == ResourceType.Train && !r.InUse);
        if (idle is null) return null;

        var activeLines = snapshot.Lines.Where(l => !l.PendingRemoval).ToList();
        if (activeLines.Count == 0) return null;

        // Engine rejects deployment at any occupied tile (including PendingRemoval trains)
        var occupied = snapshot.Trains
            .Select(t => t.TilePosition)
            .ToHashSet();

        var waitingPerStation = snapshot.Passengers
            .Where(p => p.StationId.HasValue)
            .GroupBy(p => p.StationId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        // Prioritize lines with 0 active trains to break steal→deploy deadlocks
        var trainlessLines = activeLines
            .Where(l => !snapshot.Trains.Any(t => t.LineId == l.LineId && !t.PendingRemoval))
            .ToList();

        var targetLines = trainlessLines.Count > 0 ? trainlessLines : activeLines;

        var candidates = targetLines
            .SelectMany(l => l.StationIds.Select(s => (l.LineId, StationId: s)))
            .Where(x => _stations.ContainsKey(x.StationId))
            .Where(x => !occupied.Contains(_stations[x.StationId].Location))
            .ToList();

        if (candidates.Count == 0 && trainlessLines.Count > 0)
        {
            candidates = activeLines
                .SelectMany(l => l.StationIds.Select(s => (l.LineId, StationId: s)))
                .Where(x => _stations.ContainsKey(x.StationId))
                .Where(x => !occupied.Contains(_stations[x.StationId].Location))
                .ToList();
        }

        if (candidates.Count == 0) return null;

        // Build per-line train counts for tiebreaking
        var trainsPerLine = snapshot.Trains
            .Where(t => !t.PendingRemoval)
            .GroupBy(t => t.LineId)
            .ToDictionary(g => g.Key, g => g.Count());

        var best = candidates
            .OrderByDescending(x => waitingPerStation.GetValueOrDefault(x.StationId, 0))
            .ThenBy(x => trainsPerLine.GetValueOrDefault(x.LineId, 0))
            .First();

        return new AddVehicleToLine(idle.Id, best.LineId, best.StationId);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Train rebalancing — steal from the least-busy line when needed
    // ═══════════════════════════════════════════════════════════════════════

    private PlayerAction? TryStealTrain(GameSnapshot snapshot)
    {
        if (_pendingTrainRemoval.HasValue) return null;

        bool hasIdleTrain = snapshot.Resources.Any(r => r.Type == ResourceType.Train && !r.InUse);
        if (hasIdleTrain) return null;

        var activeLines = snapshot.Lines.Where(l => !l.PendingRemoval).ToList();

        var waitingPerStation = snapshot.Passengers
            .Where(p => p.StationId.HasValue)
            .GroupBy(p => p.StationId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        // Steal only when a line has no active trains (rule 4: "for a new line")
        bool lineWithoutTrain = activeLines.Any(l =>
            !snapshot.Trains.Any(t => t.LineId == l.LineId && !t.PendingRemoval));

        if (!lineWithoutTrain) return null;

        // Victim: line with ≥2 active trains and fewest total waiting passengers
        var victim = activeLines
            .Select(l =>
            {
                var trains = snapshot.Trains
                    .Where(t => t.LineId == l.LineId && !t.PendingRemoval)
                    .ToList();
                int waiting = l.StationIds.Sum(s => waitingPerStation.GetValueOrDefault(s, 0));
                return (Line: l, Trains: trains, Waiting: waiting);
            })
            .Where(x => x.Trains.Count >= 2)
            .OrderBy(x => x.Waiting)
            .FirstOrDefault();

        if (victim.Line is null) return null;

        // Remove the train carrying the fewest passengers
        var leastBusy = victim.Trains
            .OrderBy(t => t.Passengers.Count)
            .First();

        _pendingTrainRemoval = leastBusy.TrainId;
        return new RemoveVehicle(leastBusy.TrainId);
    }

    // ═══════════════════════════════════════════════════════════════════════

    private static int Chebyshev(Location a, Location b)
        => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

    public void OnStationSpawned(GameSnapshot snapshot, Guid stationId, Location location, StationType stationType)
    {
        _stations[stationId] = (location, stationType);
        _pendingConnections.Enqueue(stationId);
    }

    public void OnVehicleRemoved(GameSnapshot snapshot, Guid vehicleId)
    {
        if (_pendingTrainRemoval == vehicleId)
            _pendingTrainRemoval = null;
    }

    public void OnDayStart(GameSnapshot snapshot) { }
    public void OnWeeklyGiftReceived(GameSnapshot snapshot, ResourceType gift) { }
    public void OnPassengerSpawned(GameSnapshot snapshot, Guid stationId, Guid passengerId) { }
    public void OnStationCrowded(GameSnapshot snapshot, Guid stationId, int numberOfPassengersWaiting) { }
    public void OnGameOver(GameSnapshot snapshot, Guid stationId)
    {
        var station = snapshot.Stations.Values.FirstOrDefault(s => s.Id == stationId);
        int waiting = snapshot.Passengers.Count(p => p.StationId == stationId);
        int idleTrains = snapshot.Resources.Count(r => r.Type == ResourceType.Train && !r.InUse);
        int idleLines = snapshot.Resources.Count(r => r.Type == ResourceType.Line && !r.InUse);
        int pendingTrains = snapshot.Trains.Count(t => t.PendingRemoval);
        int totalTrainResources = snapshot.Resources.Count(r => r.Type == ResourceType.Train);
        int totalLineResources = snapshot.Resources.Count(r => r.Type == ResourceType.Line);
        Console.WriteLine($"  [GAME OVER] Day {snapshot.Time.Day} Hour {snapshot.Time.Hour:D2} -- station {station?.StationType} @ ({station?.Location.X},{station?.Location.Y}) has {waiting} passengers");
        Console.WriteLine($"    Total trains: {totalTrainResources}, Total lines: {totalLineResources}, Idle T: {idleTrains}, Idle L: {idleLines}, PendingRm: {pendingTrains}");
        foreach (var line in snapshot.Lines.Where(l => !l.PendingRemoval))
        {
            var trains = snapshot.Trains.Count(t => t.LineId == line.LineId && !t.PendingRemoval);
            var lineWaiting = line.StationIds.Sum(s =>
                snapshot.Passengers.Count(p => p.StationId == s));
            Console.WriteLine($"    [LINE] {line.LineId.ToString()[..8]}: {line.StationIds.Count} stn, {trains} tr, {lineWaiting} waiting");
        }
    }
    public void OnInvalidPlayerAction(GameSnapshot snapshot, int code, string description)
        => Console.WriteLine($"  [INVALID] Day {snapshot.Time.Day} Hour {snapshot.Time.Hour:D2} -- code {code}: {description}");
    public void OnLineRemoved(GameSnapshot snapshot, Guid lineId) { }
}
