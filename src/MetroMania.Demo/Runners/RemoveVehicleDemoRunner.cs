using MetroMania.Domain.Enums;
using MetroMania.Engine.Contracts;
using MetroMania.Engine.Model;

namespace MetroMania.Demo.Runners;

/// <summary>
/// Runner that demonstrates RemoveVehicle with passengers on board.
///
/// Strategy:
///   Hour 0 — Create line between Circle (3,4) and Rectangle (12,4)
///   Hour 1 — Deploy train at Circle station
///   Hours 2–9 — Do nothing; let the train pick up passengers and deliver a few
///   Hour 10 — Remove the train (it should have passengers on board by now)
///   After that — Do nothing; observe the pending-removal, force-drop, and removal event
///
/// The console output shows every event as it fires, making the full lifecycle visible.
/// </summary>
internal class RemoveVehicleDemoRunner : IMetroManiaRunner
{
    private int _tick;
    private bool _trainRemoved;

    public PlayerAction OnHourTicked(GameSnapshot snapshot)
    {
        _tick++;

        var availableLines = snapshot.Resources.Where(r => r.Type == ResourceType.Line && !r.InUse).ToList();
        var availableTrains = snapshot.Resources.Where(r => r.Type == ResourceType.Train && !r.InUse).ToList();

        // Hour 0 (tick 1): create line
        if (snapshot.Lines.Count == 0 && availableLines.Count > 0 && snapshot.Stations.Count >= 2)
        {
            var stations = snapshot.Stations.Values.ToList();
            Console.WriteLine($"  [Tick {_tick}] Creating line between {stations[0].StationType} and {stations[1].StationType}");
            return new CreateLine(availableLines[0].Id, stations[0].Id, stations[1].Id);
        }

        // Hour 1 (tick 2): deploy train
        if (snapshot.Trains.Count == 0 && availableTrains.Count > 0 && snapshot.Lines.Count > 0 && !_trainRemoved)
        {
            var line = snapshot.Lines[0];
            Console.WriteLine($"  [Tick {_tick}] Deploying train on line at station {line.StationIds[0]:N}");
            return new AddVehicleToLine(availableTrains[0].Id, line.LineId, line.StationIds[0]);
        }

        // Hour 10 (tick 11): remove the train
        if (_tick == 11 && snapshot.Trains.Count > 0)
        {
            var train = snapshot.Trains[0];
            var paxCount = train.Passengers.Count;
            Console.WriteLine($"  [Tick {_tick}] *** Removing train {train.TrainId:N} with {paxCount} passenger(s) on board ***");
            return new RemoveVehicle(train.TrainId);
        }

        // Log train status each tick after removal request
        if (_tick > 11 && snapshot.Trains.Count > 0)
        {
            var train = snapshot.Trains[0];
            Console.WriteLine($"  [Tick {_tick}] Train pending={train.PendingRemoval}, passengers={train.Passengers.Count}, pos={train.TilePosition}");
        }

        return new NoAction();
    }

    public void OnDayStart(GameSnapshot snapshot)
    {
        Console.WriteLine($"  ── Day {snapshot.Time.Day} ({snapshot.Time.DayOfWeek}) ──");
    }

    public void OnWeeklyGiftReceived(GameSnapshot snapshot, ResourceType gift)
    {
        Console.WriteLine($"  [Gift] Received {gift}");
    }

    public void OnStationSpawned(GameSnapshot snapshot, Guid stationId, Location location, StationType stationType)
    {
        Console.WriteLine($"  [Station] {stationType} spawned at {location}");
    }

    public void OnPassengerSpawned(GameSnapshot snapshot, Guid stationId, Guid passengerId)
    {
        var passenger = snapshot.Passengers.First(p => p.Id == passengerId);
        Console.WriteLine($"  [Passenger] Spawned at station {stationId:N} wanting {passenger.DestinationType}");
    }

    public void OnStationCrowded(GameSnapshot snapshot, Guid stationId, int numberOfPassengersWaiting)
    {
        var station = snapshot.Stations.Values.First(s => s.Id == stationId);
        Console.WriteLine($"  [CROWDED] {station.StationType} station has {numberOfPassengersWaiting} passengers!");
    }

    public void OnGameOver(GameSnapshot snapshot, Guid stationId)
    {
        var station = snapshot.Stations.Values.First(s => s.Id == stationId);
        Console.WriteLine($"  [GAME OVER] {station.StationType} station overrun!");
    }

    public void OnInvalidPlayerAction(GameSnapshot snapshot, int code, string description)
    {
        Console.WriteLine($"  [ERROR {code}] {description}");
    }

    public void OnVehicleRemoved(GameSnapshot snapshot, Guid vehicleId)
    {
        _trainRemoved = true;
        var trainCount = snapshot.Trains.Count;
        var trainResourceFree = snapshot.Resources.Count(r => r.Type == ResourceType.Train && !r.InUse);
        Console.WriteLine($"  [REMOVED] Train {vehicleId:N} removed! Trains left: {trainCount}, free train resources: {trainResourceFree}");
    }

    public void OnLineRemoved(GameSnapshot snapshot, Guid lineId)
    {
        Console.WriteLine($"  [REMOVED] Line {lineId:N} removed!");
    }
}
