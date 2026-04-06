using MetroMania.Domain.Enums;
using MetroMania.Engine.Contracts;
using MetroMania.Engine.Model;

namespace MetroMania.Demo.Runners;

/// <summary>
/// Runner that demonstrates RemoveLine with 3 active trains carrying passengers.
///
/// Network:
///   Circle (2,4) ——— Rectangle (6,4) ——— Triangle (10,4) ——— Diamond (14,4)
///
/// Setup sequence (one action per tick):
///   0. Create line: Circle → Rectangle
///   1. Extend line: Rectangle → Triangle
///   2. Extend line: Triangle → Diamond
///   3. Deploy Train 1 at Circle
///   4. Deploy Train 2 at Rectangle
///   5. Deploy Train 3 at Diamond
///   6+ Do nothing — let trains run and gather passengers
///
/// At tick 35 (~Day 2 Hour 11) the runner removes the entire line.
/// By then the 3 trains should be spread across the line with passengers on board.
///
/// Console output traces every event so the full RemoveLine lifecycle is visible:
///   1. Line + all 3 trains flagged PendingRemoval
///   2. Each train drops passengers one by one (force-drop at wrong stations, score at right ones)
///   3. OnVehicleRemoved fires as each train empties
///   4. OnLineRemoved fires once all 3 trains are gone
/// </summary>
internal class RemoveLineDemoRunner : IMetroManiaRunner
{
    private readonly Dictionary<Guid, StationType> _stationTypes = new();
    private readonly Dictionary<Guid, string> _stationNames = new();
    private int _tick;
    private int _setupPhase;
    private bool _lineRemoveRequested;
    private Guid? _lineId;

    public PlayerAction OnHourTicked(GameSnapshot snapshot)
    {
        _tick++;

        var availableLines = snapshot.Resources.Where(r => r.Type == ResourceType.Line && !r.InUse).ToList();
        var availableTrains = snapshot.Resources.Where(r => r.Type == ResourceType.Train && !r.InUse).ToList();
        var stations = snapshot.Stations.Values.ToList();

        // Setup phase: build the line and deploy trains
        if (_setupPhase <= 5 && stations.Count >= 4)
        {
            switch (_setupPhase)
            {
                case 0:
                {
                    if (availableLines.Count == 0) return new NoAction();
                    var circle = stations.First(s => s.StationType == StationType.Circle);
                    var rect = stations.First(s => s.StationType == StationType.Rectangle);
                    _setupPhase++;
                    Console.WriteLine($"  [Tick {_tick}] Creating line: Circle → Rectangle");
                    return new CreateLine(availableLines[0].Id, circle.Id, rect.Id);
                }
                case 1:
                {
                    var line = snapshot.Lines[0];
                    _lineId = line.LineId;
                    var rect = stations.First(s => s.StationType == StationType.Rectangle);
                    var tri = stations.First(s => s.StationType == StationType.Triangle);
                    _setupPhase++;
                    Console.WriteLine($"  [Tick {_tick}] Extending line: Rectangle → Triangle");
                    return new ExtendLineFromTerminal(line.LineId, rect.Id, tri.Id);
                }
                case 2:
                {
                    var line = snapshot.Lines[0];
                    var tri = stations.First(s => s.StationType == StationType.Triangle);
                    var diamond = stations.First(s => s.StationType == StationType.Diamond);
                    _setupPhase++;
                    Console.WriteLine($"  [Tick {_tick}] Extending line: Triangle → Diamond");
                    return new ExtendLineFromTerminal(line.LineId, tri.Id, diamond.Id);
                }
                case 3:
                {
                    if (availableTrains.Count == 0) return new NoAction();
                    var line = snapshot.Lines[0];
                    var circle = stations.First(s => s.StationType == StationType.Circle);
                    _setupPhase++;
                    Console.WriteLine($"  [Tick {_tick}] Deploying Train 1 at Circle");
                    return new AddVehicleToLine(availableTrains[0].Id, line.LineId, circle.Id);
                }
                case 4:
                {
                    if (availableTrains.Count == 0) return new NoAction();
                    var line = snapshot.Lines[0];
                    var rect = stations.First(s => s.StationType == StationType.Rectangle);
                    _setupPhase++;
                    Console.WriteLine($"  [Tick {_tick}] Deploying Train 2 at Rectangle");
                    return new AddVehicleToLine(availableTrains[0].Id, line.LineId, rect.Id);
                }
                case 5:
                {
                    if (availableTrains.Count == 0) return new NoAction();
                    var line = snapshot.Lines[0];
                    var diamond = stations.First(s => s.StationType == StationType.Diamond);
                    _setupPhase++;
                    Console.WriteLine($"  [Tick {_tick}] Deploying Train 3 at Diamond");
                    return new AddVehicleToLine(availableTrains[0].Id, line.LineId, diamond.Id);
                }
            }
        }

        // At tick 35: remove the entire line
        if (_tick == 35 && _lineId.HasValue && !_lineRemoveRequested)
        {
            _lineRemoveRequested = true;
            var totalPax = snapshot.Trains.Sum(t => t.Passengers.Count);
            Console.WriteLine();
            Console.WriteLine($"  [Tick {_tick}] ╔══════════════════════════════════════════════════════╗");
            Console.WriteLine($"  [Tick {_tick}] ║  REMOVING LINE — {snapshot.Trains.Count} trains, {totalPax} total passengers on board  ║");
            Console.WriteLine($"  [Tick {_tick}] ╚══════════════════════════════════════════════════════╝");
            foreach (var train in snapshot.Trains)
            {
                var paxSummary = string.Join(", ", train.Passengers
                    .GroupBy(p => p.DestinationType)
                    .Select(g => $"{g.Count()}×{g.Key}"));
                Console.WriteLine($"  [Tick {_tick}]   Train {train.TrainId.ToString()[..8]} at {train.TilePosition} — {train.Passengers.Count} pax [{paxSummary}]");
            }
            Console.WriteLine();
            return new RemoveLine(_lineId.Value);
        }

        // After removal: log train status each tick
        if (_lineRemoveRequested && snapshot.Trains.Count > 0 && _tick % 1 == 0)
        {
            foreach (var train in snapshot.Trains)
            {
                var stationName = GetStationAt(snapshot, train.TilePosition);
                var atStation = stationName != null ? $" @ {stationName}" : "";
                Console.WriteLine($"  [Tick {_tick}] Train {train.TrainId.ToString()[..8]} pending={train.PendingRemoval}, pax={train.Passengers.Count}, pos={train.TilePosition}{atStation}");
            }
        }

        return new NoAction();
    }

    private string? GetStationAt(GameSnapshot snapshot, Location pos)
    {
        if (snapshot.Stations.TryGetValue(pos, out var station))
            return station.StationType.ToString();
        return null;
    }

    public void OnDayStart(GameSnapshot snapshot)
    {
        Console.WriteLine($"  ── Day {snapshot.Time.Day} ({snapshot.Time.DayOfWeek}) ── Score: {snapshot.Score}");
    }

    public void OnWeeklyGiftReceived(GameSnapshot snapshot, ResourceType gift)
    {
        Console.WriteLine($"  [Gift] Received {gift}");
    }

    public void OnStationSpawned(GameSnapshot snapshot, Guid stationId, Location location, StationType stationType)
    {
        _stationTypes[stationId] = stationType;
        _stationNames[stationId] = stationType.ToString();
        Console.WriteLine($"  [Station] {stationType} spawned at {location}");
    }

    public void OnPassengerSpawned(GameSnapshot snapshot, Guid stationId, Guid passengerId)
    {
        // Only log during the removal phase to keep output readable
        if (!_lineRemoveRequested) return;
        var passenger = snapshot.Passengers.First(p => p.Id == passengerId);
        var stationName = _stationNames.GetValueOrDefault(stationId, "?");
        Console.WriteLine($"  [Passenger] Spawned at {stationName} wanting {passenger.DestinationType}");
    }

    public void OnStationCrowded(GameSnapshot snapshot, Guid stationId, int numberOfPassengersWaiting)
    {
        var name = _stationNames.GetValueOrDefault(stationId, "?");
        Console.WriteLine($"  [CROWDED] {name} has {numberOfPassengersWaiting} passengers!");
    }

    public void OnGameOver(GameSnapshot snapshot, Guid stationId)
    {
        var name = _stationNames.GetValueOrDefault(stationId, "?");
        Console.WriteLine($"  [GAME OVER] {name} station overrun!");
    }

    public void OnInvalidPlayerAction(GameSnapshot snapshot, int code, string description)
    {
        Console.WriteLine($"  [ERROR {code}] {description}");
    }

    public void OnVehicleRemoved(GameSnapshot snapshot, Guid vehicleId)
    {
        var trainsLeft = snapshot.Trains.Count;
        Console.WriteLine($"  [VEHICLE REMOVED] Train {vehicleId.ToString()[..8]} removed! Trains remaining: {trainsLeft}");
    }

    public void OnLineRemoved(GameSnapshot snapshot, Guid lineId)
    {
        var linesLeft = snapshot.Lines.Count;
        var freeLines = snapshot.Resources.Count(r => r.Type == ResourceType.Line && !r.InUse);
        Console.WriteLine($"  [LINE REMOVED] Line {lineId.ToString()[..8]} removed! Lines remaining: {linesLeft}, free line resources: {freeLines}");
    }
}
