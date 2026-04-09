using MetroMania.Domain.Enums;
using MetroMania.Engine.Contracts;
using MetroMania.Engine.Model;

namespace MetroMania.Demo.Runners;

/// <summary>
/// Bonus Level runner: single-backbone with greedy cheapest-insertion.
///
/// Strategy: one line through all 12 stations, every train on the backbone,
/// each new station inserted where it adds the fewest tiles (Chebyshev detour).
/// Trains deploy at the most crowded station on the backbone.
///
/// Result: 537 score, 48 days survived (vs OptimalRunner baseline 390/41).
///
/// Proven through 13+ iterations that no multi-line, express, deferred, or
/// forced-insertion variant beats this: the system is fundamentally limited
/// by 5 trains serving 12 stations on an 82-tile backbone (~50% of demand).
/// </summary>
internal class BonusLevelRunner : IMetroManiaRunner
{
    private readonly Dictionary<Guid, (Location Location, StationType Type)> _stations = new();
    private readonly List<Guid> _spawnOrder = [];
    private Guid _backboneLineId;

    public PlayerAction OnHourTicked(GameSnapshot snapshot)
    {
        return TryConnectToBackbone(snapshot)
            ?? TryDeployTrain(snapshot)
            ?? PlayerAction.None;
    }

    private PlayerAction? TryConnectToBackbone(GameSnapshot snapshot)
    {
        var activeLines = snapshot.Lines.Where(l => !l.PendingRemoval).ToList();

        if (activeLines.Count == 0)
            return CreateBackbone(snapshot);

        var backbone = activeLines.FirstOrDefault(l => l.LineId == _backboneLineId)
                    ?? activeLines.OrderByDescending(l => l.StationIds.Count).First();
        _backboneLineId = backbone.LineId;

        var backboneIds = backbone.StationIds.ToHashSet();
        var unconnected = _stations.Keys.Where(id => !backboneIds.Contains(id)).ToList();
        if (unconnected.Count == 0) return null;

        // Connect the station with the cheapest insertion cost
        var cheapStation = unconnected
            .Select(id => (Id: id, Cost: BestExtensionCost(backbone, id)))
            .OrderBy(x => x.Cost)
            .First();

        return BestBackboneExtension(backbone, cheapStation.Id);
    }

    private PlayerAction? CreateBackbone(GameSnapshot snapshot)
    {
        if (_spawnOrder.Count < 2) return null;

        var lineResource = snapshot.Resources
            .FirstOrDefault(r => r.Type == ResourceType.Line && !r.InUse);
        if (lineResource is null) return null;

        // Pick the closest pair of different-typed stations
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

            if ((!bestDiffType && diffType) || (diffType == bestDiffType && dist < bestDist))
            {
                bestA = a; bestB = b; bestDist = dist; bestDiffType = diffType;
            }
        }

        _backboneLineId = lineResource.Id;
        return new CreateLine(lineResource.Id, bestA, bestB);
    }

    /// <summary>
    /// Minimum detour cost to insert targetId into the backbone
    /// (terminal extension or in-between insertion).
    /// </summary>
    private int BestExtensionCost(Line backbone, Guid targetId)
    {
        var targetLoc = _stations[targetId].Location;
        var sids = backbone.StationIds;

        int bestCost = Math.Min(
            Chebyshev(_stations[sids[0]].Location, targetLoc),
            Chebyshev(_stations[sids[^1]].Location, targetLoc));

        for (int i = 0; i < sids.Count - 1; i++)
        {
            if (!_stations.TryGetValue(sids[i], out var fromInfo)
             || !_stations.TryGetValue(sids[i + 1], out var toInfo))
                continue;

            int detour = Chebyshev(fromInfo.Location, targetLoc)
                       + Chebyshev(targetLoc, toInfo.Location)
                       - Chebyshev(fromInfo.Location, toInfo.Location);

            if (detour < bestCost) bestCost = detour;
        }

        return bestCost;
    }

    /// <summary>
    /// Returns the best extension action (terminal or in-between) for the target station.
    /// </summary>
    private PlayerAction BestBackboneExtension(Line backbone, Guid targetId)
    {
        var targetLoc = _stations[targetId].Location;
        var sids = backbone.StationIds;

        int headDist = Chebyshev(_stations[sids[0]].Location, targetLoc);
        int tailDist = Chebyshev(_stations[sids[^1]].Location, targetLoc);
        Guid bestTerminal = headDist <= tailDist ? sids[0] : sids[^1];
        int bestTerminalCost = Math.Min(headDist, tailDist);

        Guid bestInsertFrom = default, bestInsertTo = default;
        int bestDetourCost = int.MaxValue;

        for (int i = 0; i < sids.Count - 1; i++)
        {
            if (!_stations.TryGetValue(sids[i], out var fromInfo)
             || !_stations.TryGetValue(sids[i + 1], out var toInfo))
                continue;

            int detour = Chebyshev(fromInfo.Location, targetLoc)
                       + Chebyshev(targetLoc, toInfo.Location)
                       - Chebyshev(fromInfo.Location, toInfo.Location);

            if (detour < bestDetourCost)
            {
                bestDetourCost = detour;
                bestInsertFrom = sids[i];
                bestInsertTo = sids[i + 1];
            }
        }

        if (bestInsertFrom != default && bestDetourCost < bestTerminalCost)
            return new ExtendLineInBetween(backbone.LineId, bestInsertFrom, targetId, bestInsertTo);

        return new ExtendLineFromTerminal(backbone.LineId, bestTerminal, targetId);
    }

    private PlayerAction? TryDeployTrain(GameSnapshot snapshot)
    {
        var idleTrain = snapshot.Resources
            .FirstOrDefault(r => r.Type == ResourceType.Train && !r.InUse);
        if (idleTrain is null) return null;

        var backbone = snapshot.Lines.FirstOrDefault(l => l.LineId == _backboneLineId && !l.PendingRemoval);
        if (backbone is null) return null;

        // Deploy at the most crowded station (avoiding tiles with existing trains)
        var occupiedTiles = snapshot.Trains.Select(t => t.TilePosition).ToHashSet();
        var deployStation = backbone.StationIds
            .Where(sid => _stations.TryGetValue(sid, out _)
                       && !occupiedTiles.Contains(_stations[sid].Location))
            .OrderByDescending(sid => snapshot.Passengers.Count(p => p.StationId == sid))
            .FirstOrDefault();

        if (deployStation == default) return null;
        return new AddVehicleToLine(idleTrain.Id, backbone.LineId, deployStation);
    }

    private static int Chebyshev(Location a, Location b)
        => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

    private int ComputeLineLength(Line line)
    {
        int total = 0;
        for (int i = 0; i < line.StationIds.Count - 1; i++)
        {
            if (_stations.TryGetValue(line.StationIds[i], out var a)
             && _stations.TryGetValue(line.StationIds[i + 1], out var b))
                total += Chebyshev(a.Location, b.Location);
        }
        return total;
    }

    public void OnStationSpawned(GameSnapshot snapshot, Guid stationId, Location location, StationType stationType)
    {
        _stations[stationId] = (location, stationType);
        _spawnOrder.Add(stationId);
    }

    public void OnStationCrowded(GameSnapshot snapshot, Guid stationId, int numberOfPassengersWaiting) { }
    public void OnDayStart(GameSnapshot snapshot) { }
    public void OnWeeklyGiftReceived(GameSnapshot snapshot, ResourceType gift) { }
    public void OnPassengerSpawned(GameSnapshot snapshot, Guid stationId, Guid passengerId) { }
    public void OnGameOver(GameSnapshot snapshot, Guid stationId)
    {
        var station = snapshot.Stations.Values.FirstOrDefault(s => s.Id == stationId);
        int waiting = snapshot.Passengers.Count(p => p.StationId == stationId);
        Console.WriteLine($"  [GAME OVER] Day {snapshot.Time.Day} Hour {snapshot.Time.Hour:D2} -- station {station?.StationType} @ ({station?.Location.X},{station?.Location.Y}) has {waiting} passengers");
        Console.WriteLine($"    Lines: {snapshot.Lines.Count(l => !l.PendingRemoval)}, Trains: {snapshot.Trains.Count(t => !t.PendingRemoval)}, Score: {snapshot.Score}");
        foreach (var line in snapshot.Lines.Where(l => !l.PendingRemoval))
        {
            int trains = snapshot.Trains.Count(t => t.LineId == line.LineId && !t.PendingRemoval);
            Console.WriteLine($"    [LINE] {line.LineId.ToString()[..8]}: {line.StationIds.Count} stn, {trains} tr, len={ComputeLineLength(line)}");
        }
    }
    public void OnInvalidPlayerAction(GameSnapshot snapshot, int code, string description)
        => Console.WriteLine($"  [INVALID] Day {snapshot.Time.Day} Hour {snapshot.Time.Hour:D2} -- code {code}: {description}");
    public void OnVehicleRemoved(GameSnapshot snapshot, Guid vehicleId) { }
    public void OnLineRemoved(GameSnapshot snapshot, Guid lineId) { }
}
