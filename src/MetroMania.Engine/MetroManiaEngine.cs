using System.Diagnostics;
using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;
using MetroMania.Engine.Contracts;
using MetroMania.Engine.Model;

namespace MetroMania.Engine;

public class MetroManiaEngine
{
    public GameResult Run(IMetroManiaRunner runner, Level level, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var sim = RunSimulation(runner, level, cancellationToken: cancellationToken);
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
    public GameSnapshot RunForHours(IMetroManiaRunner runner, Level level, int targetHours, CancellationToken cancellationToken = default)
    {
        var sim = RunSimulation(runner, level, targetHours, cancellationToken);
        return CreateSnapshot(sim.Time, sim.HoursElapsed, sim.GameOver, sim.ActiveStations, sim.TotalScore);
    }

    private SimulationResult RunSimulation(IMetroManiaRunner runner, Level level, int? targetHours = null, CancellationToken cancellationToken = default)
    {
        var random = new Random(level.LevelData.Seed);
        var activeStations = new Dictionary<Location, StationState>();
        var stationTypes = Enum.GetValues<StationType>();

        int totalPassengersSpawned = 0;
        int totalScore = 0;
        int hoursElapsed = 0;
        var time = new GameTime(0, 0, default);
        bool gameOver = false;

        while (!gameOver && (targetHours is null || hoursElapsed < targetHours))
        {
            cancellationToken.ThrowIfCancellationRequested();
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
                        var stationId = NextGuid(random);
                        activeStations[location] = new StationState(stationId, spawn.StationType, spawn.PassengerSpawnPhases, day);
                        runner.OnStationSpawned(
                            CreateSnapshot(time, hoursElapsed, gameOver, activeStations, totalScore),
                            stationId, location, spawn.StationType);
                    }
                }

                if (dayOfWeek == DayOfWeek.Monday)
                {
                    int weekNumber = (day - 1) / 7 + 1;
                    var giftOverride = level.LevelData.WeeklyGiftOverrides
                        .FirstOrDefault(g => g.Week == weekNumber);

                    var gift = giftOverride is not null
                        ? giftOverride.ResourceType
                        : (ResourceType)random.Next(3);

                    runner.OnWeeklyGift(
                        CreateSnapshot(time, hoursElapsed, gameOver, activeStations, totalScore),
                        gift);
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

                    runner.OnPassengerWaiting(
                        CreateSnapshot(time, hoursElapsed, gameOver, activeStations, totalScore),
                        location, state.Passengers.AsReadOnly());
                }
            }

            foreach (var (location, state) in activeStations)
            {
                if (state.Passengers.Count >= 20)
                {
                    gameOver = true;
                    runner.OnGameOver(
                        CreateSnapshot(time, hoursElapsed, true, activeStations, totalScore),
                        location, state.Passengers.AsReadOnly());
                    break;
                }

                if (state.Passengers.Count >= 10)
                {
                    runner.OnStationOverrun(
                        CreateSnapshot(time, hoursElapsed, gameOver, activeStations, totalScore),
                        location, state.Passengers.AsReadOnly());
                }
            }

            if (gameOver) break;

            // Phase 2: OnDayStart — fire second
            if (isDayStart)
            {
                runner.OnDayStart(
                    CreateSnapshot(time, hoursElapsed, gameOver, activeStations, totalScore));
            }

            // Phase 3: OnHourTick — fire last
            runner.OnHourTick(
                CreateSnapshot(time, hoursElapsed, gameOver, activeStations, totalScore));
            hoursElapsed++;
        }

        return new SimulationResult(time, hoursElapsed, gameOver, totalPassengersSpawned, totalScore, activeStations);
    }

    private static Guid NextGuid(Random random)
    {
        Span<byte> bytes = stackalloc byte[16];
        random.NextBytes(bytes);
        return new Guid(bytes);
    }

    private static GameSnapshot CreateSnapshot(
        GameTime time, int hoursElapsed, bool gameOver,
        Dictionary<Location, StationState> activeStations, int totalScore)
    {
        return new GameSnapshot
        {
            Time = time,
            TotalHoursElapsed = hoursElapsed,
            GameOver = gameOver,
            TotalScore = totalScore,
            Stations = activeStations.ToDictionary(
                kvp => kvp.Key,
                kvp => new StationSnapshot
                {
                    Id = kvp.Value.Id,
                    Type = kvp.Value.Type,
                    Passengers = [.. kvp.Value.Passengers]
                })
        };
    }

    private record SimulationResult(
        GameTime Time,
        int HoursElapsed,
        bool GameOver,
        int TotalPassengersSpawned,
        int TotalScore,
        Dictionary<Location, StationState> ActiveStations);

    private class StationState(Guid id, StationType type, List<PassengerSpawnPhase> phases, int spawnedOnDay)
    {
        public Guid Id { get; } = id;
        public StationType Type { get; } = type;
        public List<PassengerSpawnPhase> Phases { get; } = phases;
        public int SpawnedOnDay { get; } = spawnedOnDay;
        public int SpawnedAtHour { get; } = (spawnedOnDay - 1) * 24;
        public List<Passenger> Passengers { get; } = [];
    }
}