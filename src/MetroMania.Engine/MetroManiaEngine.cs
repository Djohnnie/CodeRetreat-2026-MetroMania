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
            DaysSurvived = sim.Time.Day,
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
            Time = sim.Time,
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
        var time = new GameTime(0, 0, default);
        bool gameOver = false;

        while (!gameOver && (targetHours is null || hoursElapsed < targetHours))
        {
            int day = hoursElapsed / 24 + 1;
            int currentHour = hoursElapsed % 24;
            var dayOfWeek = (DayOfWeek)(day % 7);
            time = new GameTime(day, currentHour, dayOfWeek);
            bool isDayStart = currentHour == 0;

            // Phase 1: "Other events" — fire first
            if (isDayStart)
            {
                foreach (var spawn in level.LevelData.Stations)
                {
                    var location = new Location(spawn.GridX, spawn.GridY);
                    if (day == spawn.SpawnDelayDays + 1 && !activeStations.ContainsKey(location))
                    {
                        activeStations[location] = new StationState(spawn.StationType, spawn.PassengerSpawnPhases, day);
                        runner.OnStationSpawned(time, location, spawn.StationType);
                    }
                }

                if (dayOfWeek == DayOfWeek.Monday)
                {
                    var gift = (ResourceType)random.Next(3);
                    runner.OnWeeklyGift(time, gift);
                }
            }

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

                    runner.OnPassengerWaiting(time, location, state.Passengers.AsReadOnly());
                }
            }

            foreach (var (location, state) in activeStations)
            {
                if (state.Passengers.Count >= 20)
                {
                    runner.OnGameOver(time, location, state.Passengers.AsReadOnly());
                    gameOver = true;
                    break;
                }

                if (state.Passengers.Count >= 10)
                {
                    runner.OnStationOverrun(time, location, state.Passengers.AsReadOnly());
                }
            }

            if (gameOver) break;

            // Phase 2: OnDayStart — fire second
            if (isDayStart)
            {
                runner.OnDayStart(time);
            }

            // Phase 3: OnHourTick — fire last
            runner.OnHourTick(time);
            hoursElapsed++;
        }

        return new SimulationResult(time, hoursElapsed, gameOver, totalPassengersSpawned, activeStations);
    }

    private record SimulationResult(
        GameTime Time,
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