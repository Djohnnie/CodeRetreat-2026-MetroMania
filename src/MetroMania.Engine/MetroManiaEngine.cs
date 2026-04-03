using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;
using MetroMania.Engine.Contracts;
using MetroMania.Engine.Model;
using System.Diagnostics;

namespace MetroMania.Engine;

public class MetroManiaEngine
{
    public GameResult Run(IMetroManiaRunner runner, Level level, int? maxHours = null, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var simulationResult = RunSimulation(runner, level, maxHours, cancellationToken);
        stopwatch.Stop();

        return new GameResult
        {
            TotalScore = simulationResult.TotalScore,
            ProcessingTime = stopwatch.Elapsed,
            DaysSurvived = simulationResult.DaysSurvived,
            TotalPassengersSpawned = simulationResult.TotalPassengersSpawned,
            NumberOfPlayerActions = simulationResult.NumberOfPlayerActions,
            GameSnapshots = simulationResult.GameSnapshots
        };
    }

    public SimulationResult RunSimulation(IMetroManiaRunner runner, Level level, int? maxHours = null, CancellationToken cancellationToken = default)
    {
        var absoluteHour = 0;
        var snapshots = new List<GameSnapshot>();

        var snapshot = new GameSnapshot
        {
            Time = new GameTime(1, 0, DayOfWeek.Sunday),
            TotalHoursElapsed = 0,
            Score = 0,
            Resources = new List<Resource>(),
            Stations = new Dictionary<Location, Station>(),
            Lines = new List<Line>(),
            Trains = new List<Train>(),
            Passengers = new List<Passenger>(),
            LastAction = new NoAction()
        };

        // Run while not cancelled and within max hours if specified
        while (!cancellationToken.IsCancellationRequested)
        {
            if (maxHours.HasValue && absoluteHour >= maxHours.Value)
            {
                break;
            }

            var day = absoluteHour / 24 + 1;
            var hourOfDay = absoluteHour % 24;
            var dayOfWeek = (DayOfWeek)(absoluteHour / 24 % 7);
            var gameTime = new GameTime(day, hourOfDay, dayOfWeek);

            snapshot = snapshot with
            {
                Time = gameTime,
                TotalHoursElapsed = absoluteHour,
                Score = snapshot.Score, // carried forward; incremented by ProcessTrains
                Resources = [.. snapshot.Resources],
                Stations = new Dictionary<Location, Station>(snapshot.Stations),
                Lines = [.. snapshot.Lines],
                Trains = [.. snapshot.Trains],
                Passengers = [.. snapshot.Passengers]
            };

            if (hourOfDay == 0)
            {
                runner.OnDayStart(snapshot);
            }

            // Spawn stations at the start of the hour before player action
            foreach (var station in SpawnStations(level, snapshot))
            {
                runner.OnStationSpawned(snapshot, station.Id, station.Location, station.StationType);
            }

            var spawnedPassengers = SpawnPassengers(level, snapshot).ToList();
            if (spawnedPassengers.Count > 0)
                snapshot = snapshot with { Passengers = [.. snapshot.Passengers, .. spawnedPassengers.Select(p => p.Passenger)] };
            foreach (var (stationId, passenger) in spawnedPassengers)
                runner.OnPassengerSpawned(snapshot, stationId, passenger.Id);

            snapshot = ProcessTrains(level, snapshot);

            // Give weekly gift
            if (dayOfWeek == DayOfWeek.Monday && hourOfDay == 0)
            {
                var weeklyGift = GetWeeklyGift(level, snapshot);
                snapshot = snapshot with
                {
                    Resources = [.. snapshot.Resources, new Resource { Type = weeklyGift, InUse = false }]
                };
                runner.OnWeeklyGiftReceived(snapshot, weeklyGift);
            }

            // Get player action for the hour
            var playerAction = runner.OnHourTicked(snapshot);

            snapshot = ApplyPlayerAction(snapshot with { LastAction = playerAction });

            snapshots.Add(snapshot);
            absoluteHour++;
        }

        return new SimulationResult
        {
            TotalScore = snapshots.Count > 0 ? snapshots[^1].Score : 0,
            DaysSurvived = absoluteHour / 24,
            TotalPassengersSpawned = 0,
            NumberOfPlayerActions = snapshots.Count(x => x.LastAction is not NoAction),
            GameSnapshots = snapshots
        };
    }

    private static IEnumerable<Station> SpawnStations(Level level, GameSnapshot snapshot)
    {
        var newlySpawned = new List<Station>();

        foreach (var metroStation in level.LevelData.Stations)
        {
            var spawnDay = metroStation.SpawnDelayDays + 1;

            if (snapshot.Time.Day != spawnDay || snapshot.Time.Hour != 0)
                continue;

            var location = new Location(metroStation.GridX, metroStation.GridY);
            var station = new Station { Id = Guid.NewGuid(), Location = location, StationType = metroStation.StationType };

            snapshot.Stations[location] = station;
            newlySpawned.Add(station);
        }

        return newlySpawned;
    }

    private static IEnumerable<(Guid StationId, Passenger Passenger)> SpawnPassengers(Level level, GameSnapshot snapshot)
    {
        var spawnedTypes = snapshot.Stations.Values
            .Select(s => s.StationType)
            .Distinct()
            .ToArray();

        foreach (var (location, station) in snapshot.Stations)
        {
            var metroStation = level.LevelData.Stations
                .FirstOrDefault(s => s.GridX == location.X && s.GridY == location.Y);

            if (metroStation is null)
                continue;

            var hoursAlive = snapshot.TotalHoursElapsed - metroStation.SpawnDelayDays * 24;
            if (hoursAlive < 0)
                continue;

            var daysAlive = hoursAlive / 24;

            var activePhase = metroStation.PassengerSpawnPhases
                .Where(p => p.AfterDays <= daysAlive)
                .MaxBy(p => p.AfterDays);

            if (activePhase is null || activePhase.FrequencyInHours <= 0)
                continue;

            if (hoursAlive % activePhase.FrequencyInHours != 0)
                continue;

            var otherTypes = spawnedTypes.Where(t => t != station.StationType).ToArray();
            if (otherTypes.Length == 0)
                continue;

            var rng = new Random(level.LevelData.Seed + snapshot.TotalHoursElapsed * 100 + location.X * 10 + location.Y);
            var destinationType = otherTypes[rng.Next(otherTypes.Length)];

            yield return (station.Id, new Passenger(destinationType, snapshot.TotalHoursElapsed) { StationId = station.Id });
        }
    }

    /// <summary>
    /// Processes all trains for one hour tick:
    /// - If a train is at a station and has passengers to drop off, drop one (earn 1 point) and stay.
    /// - Else if a train is at a station with passengers it can deliver further on the line, pick one up and stay.
    /// - Otherwise, move the train one tile (reversing at terminals).
    /// Only one passenger action (drop or pick-up) happens per train per hour.
    /// </summary>
    private static GameSnapshot ProcessTrains(Level level, GameSnapshot snapshot)
    {
        if (snapshot.Trains.Count == 0)
            return snapshot;

        var stationLocations = snapshot.Stations
            .ToDictionary(kvp => kvp.Value.Id, kvp => kvp.Key);

        // Work on mutable copies; we commit everything at the end in one with-expression.
        var trains = snapshot.Trains.ToList();
        var waitingPassengers = snapshot.Passengers.ToList();
        int pointsScored = 0;

        for (int t = 0; t < trains.Count; t++)
        {
            var train = trains[t];

            var line = snapshot.Lines.FirstOrDefault(l => l.LineId == train.LineId);
            if (line is null) continue;

            var tilePath = LinePathHelper.ComputeTilePath(line, stationLocations);
            if (tilePath.Count == 0) continue;

            // ── At a station? ──────────────────────────────────────────────────
            if (snapshot.Stations.TryGetValue(train.TilePosition, out var currentStation))
            {
                // 1. Drop off one passenger whose destination matches this station type.
                var toDrop = train.Passengers.FirstOrDefault(p => p.DestinationType == currentStation.StationType);
                if (toDrop is not null)
                {
                    trains[t] = train with
                    {
                        Passengers = train.Passengers.Where(p => p.Id != toDrop.Id).ToList()
                    };
                    pointsScored++;
                    continue; // train stays this tick
                }

                // 2. Pick up one waiting passenger deliverable in the current travel direction.
                //    FIFO: sort by spawn time so the oldest waiting passenger boards first.
                //    Terminal edge-case: if we're at a terminal the next move will flip direction,
                //    so use the outgoing (reversed) direction for reachability check.
                int currentIndex = tilePath.IndexOf(train.TilePosition);
                // Use the outgoing direction: if the current direction would step off the path
                // (train needs to reverse), flip it. This handles both the terminal edge case
                // (a travelling train about to reverse) and a newly deployed train at index 0
                // whose direction already points into the path (no flip needed).
                bool wouldStepOffPath = currentIndex + train.Direction < 0 || currentIndex + train.Direction >= tilePath.Count;
                int effectiveDirection = wouldStepOffPath ? -train.Direction : train.Direction;
                var futureTypes = GetFutureStationTypes(tilePath, currentIndex, effectiveDirection, snapshot.Stations);

                var toPickUp = waitingPassengers
                    .Where(p =>
                        p.StationId == currentStation.Id &&
                        train.Passengers.Count < level.LevelData.VehicleCapacity &&
                        futureTypes.Contains(p.DestinationType))
                    .MinBy(p => p.SpawnedAtHour);

                if (toPickUp is not null)
                {
                    waitingPassengers.Remove(toPickUp);
                    trains[t] = train with
                    {
                        Passengers = [.. train.Passengers, toPickUp with { StationId = null }]
                    };
                    continue; // train stays this tick
                }
            }

            // ── Move one tile ──────────────────────────────────────────────────
            if (tilePath.Count < 2) continue;

            int idx = tilePath.IndexOf(train.TilePosition);
            if (idx == -1)
            {
                trains[t] = train with { TilePosition = tilePath[0], Direction = 1 };
                continue;
            }

            int dir = train.Direction;
            int nextIdx = idx + dir;
            if (nextIdx < 0 || nextIdx >= tilePath.Count)
            {
                dir = -dir;
                nextIdx = idx + dir;
            }
            nextIdx = Math.Clamp(nextIdx, 0, tilePath.Count - 1);

            trains[t] = train with { TilePosition = tilePath[nextIdx], Direction = dir };
        }

        return snapshot with
        {
            Trains    = trains,
            Passengers = waitingPassengers,
            Score     = snapshot.Score + pointsScored,
        };
    }

    /// <summary>
    /// Returns the set of station types reachable from the current tile index moving in
    /// <paramref name="direction"/> without reversing, i.e. stations the train will visit
    /// before reaching its current terminal.
    /// </summary>
    private static HashSet<StationType> GetFutureStationTypes(
        List<Location> tilePath, int currentIndex, int direction,
        Dictionary<Location, Station> stations)
    {
        var types = new HashSet<StationType>();
        int step = direction >= 0 ? 1 : -1;
        for (int i = currentIndex + step; i >= 0 && i < tilePath.Count; i += step)
        {
            if (stations.TryGetValue(tilePath[i], out var station))
                types.Add(station.StationType);
        }
        return types;
    }

    private static ResourceType GetWeeklyGift(Level level, GameSnapshot snapshot)
    {
        var weekNumber = snapshot.TotalHoursElapsed / (24 * 7) + 1;

        var overrride = level.LevelData.WeeklyGiftOverrides.FirstOrDefault(x => x.Week == weekNumber);
        if (overrride is not null)
            return overrride.ResourceType;

        var rng = new Random(level.LevelData.Seed + weekNumber);
        return rng.Next(2) == 0 ? ResourceType.Line : ResourceType.Train;
    }

    private static GameSnapshot ApplyPlayerAction(GameSnapshot snapshot) => snapshot.LastAction switch
    {
        CreateLine createLine => ApplyCreateLine(snapshot, createLine),
        AddVehicleToLine addVehicle => ApplyAddVehicleToLine(snapshot, addVehicle),
        _ => snapshot
    };

    private static GameSnapshot ApplyCreateLine(GameSnapshot snapshot, CreateLine action)
    {
        var resource = snapshot.Resources.FirstOrDefault(r => r.Id == action.LineId && r.Type == ResourceType.Line);
        if (resource is null)
            return snapshot;

        var existingLine = snapshot.Lines.FirstOrDefault(l => l.LineId == action.LineId);

        if (existingLine is null)
        {
            // Resource not yet in use — create a new line and mark the resource as used
            if (resource.InUse)
                return snapshot;

            var newLine = new Line { LineId = action.LineId, StationIds = [action.FromStationId, action.ToStationId] };
            var updatedResource = resource with { InUse = true };
            return snapshot with
            {
                Lines = [.. snapshot.Lines, newLine],
                Resources = [.. snapshot.Resources.Where(r => r.Id != resource.Id), updatedResource],
            };
        }

        // Line already exists — try to extend it if one end matches FromStationId
        var stationIds = existingLine.StationIds.ToList();

        if (stationIds[^1] == action.FromStationId)
            stationIds.Add(action.ToStationId);
        else if (stationIds[0] == action.FromStationId)
            stationIds.Insert(0, action.ToStationId);
        else
            return snapshot; // FromStationId is not at either end — ignore

        var extendedLine = existingLine with { StationIds = stationIds };
        return snapshot with
        {
            Lines = [.. snapshot.Lines.Where(l => l.LineId != action.LineId), extendedLine],
        };
    }

    private static GameSnapshot ApplyAddVehicleToLine(GameSnapshot snapshot, AddVehicleToLine action)
    {
        // Must be an available (not in-use) Train resource
        var resource = snapshot.Resources.FirstOrDefault(
            r => r.Id == action.VehicleId && r.Type == ResourceType.Train && !r.InUse);
        if (resource is null)
            return snapshot;

        // Line must exist and be in use
        var line = snapshot.Lines.FirstOrDefault(l => l.LineId == action.LineId);
        if (line is null)
            return snapshot;

        // The spawn station must be on the line
        if (!line.StationIds.Contains(action.StationId))
            return snapshot;

        // Resolve the station to a tile location
        var stationEntry = snapshot.Stations.FirstOrDefault(kvp => kvp.Value.Id == action.StationId);
        if (stationEntry.Value is null)
            return snapshot;

        var newTrain = new Train
        {
            TrainId = action.VehicleId,
            LineId = action.LineId,
            TilePosition = stationEntry.Key,
            Direction = 1,
        };

        return snapshot with
        {
            Trains = [.. snapshot.Trains, newTrain],
            Resources = [.. snapshot.Resources.Where(r => r.Id != resource.Id), resource with { InUse = true }],
        };
    }
}