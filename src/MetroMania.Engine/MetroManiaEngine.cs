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
        PlayerAction lastPlayerAction = new NoAction();
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
            LastAction = lastPlayerAction
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
                Score = 0, // TODO: calculate score based on game state
                Resources = [.. snapshot.Resources],
                Stations = new Dictionary<Location, Station>(snapshot.Stations),
                Lines = [.. snapshot.Lines],
                Trains = [.. snapshot.Trains],
                Passengers = [.. snapshot.Passengers],
                LastAction = lastPlayerAction
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

            MoveTrains(level, snapshot);

            TransportPassengers(level, snapshot);

            // Give weekly gift at the start of Monday (before player action)
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
            lastPlayerAction = runner.OnHourTicked(snapshot);

            snapshot = ApplyPlayerAction(snapshot, lastPlayerAction);

            snapshots.Add(snapshot);
            absoluteHour++;
        }

        return new SimulationResult
        {
            TotalScore = 0,
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

    private static void MoveTrains(Level level, GameSnapshot snapshot)
    {
        // Not yet implemented
    }

    private static void TransportPassengers(Level level, GameSnapshot snapshot)
    {
        // Not yet implemented
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

    private static GameSnapshot ApplyPlayerAction(GameSnapshot snapshot, PlayerAction action) => action switch
    {
        CreateLine createLine => ApplyCreateLine(snapshot, createLine),
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
                Lines     = [.. snapshot.Lines, newLine],
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
}