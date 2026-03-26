using System.Diagnostics;
using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;
using MetroMania.Engine.Contracts;
using MetroMania.Engine.Model;

namespace MetroMania.Engine;

public class MetroManiaEngine
{
    public GameResult Run(IMetroManiaRunner runner, Level level)
    {
        var stopwatch = Stopwatch.StartNew();
        var sim = RunSimulation(runner, level);
        stopwatch.Stop();

        return new GameResult
        {
            Score = sim.HoursElapsed,
            TimeTaken = stopwatch.Elapsed,
            DaysSurvived = sim.Day,
            TotalPassengersSpawned = sim.TotalPassengersSpawned
        };
    }

    /// <summary>
    /// Runs the game simulation for a specific number of hours and returns a snapshot of the state.
    /// If the game ends before reaching the target hours, the snapshot reflects the game-over state.
    /// </summary>
    public GameSnapshot RunForHours(IMetroManiaRunner runner, Level level, int targetHours)
    {
        var sim = RunSimulation(runner, level, targetHours);

        return new GameSnapshot
        {
            Day = sim.Day,
            Hour = sim.Hour,
            TotalHoursElapsed = sim.HoursElapsed,
            GameOver = sim.GameOver,
            Stations = sim.ActiveStations.ToDictionary(
                kvp => kvp.Key,
                kvp => new StationSnapshot
                {
                    Type = kvp.Value.Type,
                    Passengers = [.. kvp.Value.Passengers]
                })
        };
    }

    private SimulationResult RunSimulation(IMetroManiaRunner runner, Level level, int? targetHours = null)
    {
        var random = new Random(level.LevelData.Seed);
        var activeStations = new Dictionary<Location, StationState>();
        var stationTypes = Enum.GetValues<StationType>();

        int totalPassengersSpawned = 0;
        int hoursElapsed = 0;
        int day = 0;
        int currentHour = 0;
        bool gameOver = false;

        while (!gameOver && (targetHours is null || hoursElapsed < targetHours))
        {
            day++;
            var dayOfWeek = (DayOfWeek)(day % 7);

            runner.OnDayStart(day, dayOfWeek);

            if (dayOfWeek == DayOfWeek.Monday)
            {
                var gift = (ResourceType)random.Next(3);
                runner.OnWeeklyGift(gift);
            }

            foreach (var spawn in level.LevelData.Stations)
            {
                var location = new Location(spawn.GridX, spawn.GridY);
                if (spawn.SpawnDelayDays == day && !activeStations.ContainsKey(location))
                {
                    activeStations[location] = new StationState(spawn.StationType, spawn.PassengerSpawnPhases, day);
                    runner.OnStationSpawned(location, spawn.StationType);
                }
            }

            for (currentHour = 0; currentHour < 24; currentHour++)
            {
                if (targetHours is not null && hoursElapsed >= targetHours)
                    break;

                int totalHour = (day - 1) * 24 + currentHour;

                foreach (var (location, state) in activeStations)
                {
                    int daysSinceSpawn = day - state.SpawnedOnDay;
                    var activePhase = state.Phases
                        .Where(p => daysSinceSpawn >= p.AfterDays)
                        .OrderByDescending(p => p.AfterDays)
                        .FirstOrDefault();

                    if (activePhase is null)
                        continue;

                    if ((totalHour - state.SpawnedAtHour) % activePhase.FrequencyInHours == 0
                        && totalHour > state.SpawnedAtHour)
                    {
                        StationType destType;
                        do { destType = stationTypes[random.Next(stationTypes.Length)]; }
                        while (destType == state.Type);

                        state.Passengers.Add(new Passenger(destType));
                        totalPassengersSpawned++;

                        runner.OnPassengerWaiting(location, state.Passengers.AsReadOnly());
                    }
                }

                runner.OnHourTick(day, currentHour);
                hoursElapsed++;

                foreach (var (location, state) in activeStations)
                {
                    if (state.Passengers.Count >= 20)
                    {
                        runner.OnGameOver(location, state.Passengers.AsReadOnly());
                        gameOver = true;
                        break;
                    }

                    if (state.Passengers.Count >= 10)
                    {
                        runner.OnStationOverrun(location, state.Passengers.AsReadOnly());
                    }
                }

                if (gameOver) break;
            }
        }

        return new SimulationResult(day, currentHour, hoursElapsed, gameOver, totalPassengersSpawned, activeStations);
    }

    private record SimulationResult(
        int Day,
        int Hour,
        int HoursElapsed,
        bool GameOver,
        int TotalPassengersSpawned,
        Dictionary<Location, StationState> ActiveStations);

    private class StationState(StationType type, List<PassengerSpawnPhase> phases, int spawnedOnDay)
    {
        public StationType Type { get; } = type;
        public List<PassengerSpawnPhase> Phases { get; } = phases;
        public int SpawnedOnDay { get; } = spawnedOnDay;
        public int SpawnedAtHour { get; } = (spawnedOnDay - 1) * 24;
        public List<Passenger> Passengers { get; } = [];
    }
}