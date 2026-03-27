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
        return CreateSnapshot(sim.Time, sim.HoursElapsed, sim.GameOver, sim.ActiveStations, sim.TotalScore, sim.Resources);
    }

    private SimulationResult RunSimulation(IMetroManiaRunner runner, Level level, int? targetHours = null, CancellationToken cancellationToken = default)
    {
        var random = new Random(level.LevelData.Seed);
        var activeStations = new Dictionary<Location, StationState>();
        var stationTypes = Enum.GetValues<StationType>();

        // Initialize starting resources: 1 line and 1 vehicle
        var resources = new List<ResourceState>
        {
            new(NextGuid(random), ResourceType.Line),
            new(NextGuid(random), ResourceType.Train)
        };

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
                            CreateSnapshot(time, hoursElapsed, gameOver, activeStations, totalScore, resources),
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

                    resources.Add(new ResourceState(NextGuid(random), gift));

                    runner.OnWeeklyGift(
                        CreateSnapshot(time, hoursElapsed, gameOver, activeStations, totalScore, resources),
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
                        CreateSnapshot(time, hoursElapsed, gameOver, activeStations, totalScore, resources),
                        location, state.Passengers.AsReadOnly());
                }
            }

            foreach (var (location, state) in activeStations)
            {
                if (state.Passengers.Count >= 20)
                {
                    gameOver = true;
                    runner.OnGameOver(
                        CreateSnapshot(time, hoursElapsed, true, activeStations, totalScore, resources),
                        location, state.Passengers.AsReadOnly());
                    break;
                }

                if (state.Passengers.Count >= 10)
                {
                    runner.OnStationOverrun(
                        CreateSnapshot(time, hoursElapsed, gameOver, activeStations, totalScore, resources),
                        location, state.Passengers.AsReadOnly());
                }
            }

            if (gameOver) break;

            // Phase 2: OnDayStart — fire second
            if (isDayStart)
            {
                runner.OnDayStart(
                    CreateSnapshot(time, hoursElapsed, gameOver, activeStations, totalScore, resources));
            }

            // Phase 3: OnHourTick — fire last, then process the player's action
            var action = runner.OnHourTick(
                CreateSnapshot(time, hoursElapsed, gameOver, activeStations, totalScore, resources));
            ProcessAction(action, resources);
            hoursElapsed++;
        }

        return new SimulationResult(time, hoursElapsed, gameOver, totalPassengersSpawned, totalScore, activeStations, resources);
    }

    private static void ProcessAction(PlayerAction action, List<ResourceState> resources)
    {
        switch (action)
        {
            case CreateLine createLine:
                var availableLine = resources.FirstOrDefault(r => r.Type == ResourceType.Line && !r.InUse);
                if (availableLine is not null)
                {
                    availableLine.InUse = true;
                    availableLine.AssignedToLineId = createLine.LineId;
                }
                break;

            case AddVehicle addVehicle:
                var availableVehicle = resources.FirstOrDefault(r => r.Type is ResourceType.Train or ResourceType.Wagon && !r.InUse);
                if (availableVehicle is not null)
                {
                    availableVehicle.InUse = true;
                    availableVehicle.AssignedToLineId = addVehicle.LineId;
                }
                break;

            case RemoveLine removeLine:
                foreach (var resource in resources.Where(r => r.AssignedToLineId == removeLine.LineId))
                {
                    resource.InUse = false;
                    resource.AssignedToLineId = null;
                }
                break;
        }
    }

    private static Guid NextGuid(Random random)
    {
        Span<byte> bytes = stackalloc byte[16];
        random.NextBytes(bytes);
        return new Guid(bytes);
    }

    private static GameSnapshot CreateSnapshot(
        GameTime time, int hoursElapsed, bool gameOver,
        Dictionary<Location, StationState> activeStations, int totalScore,
        List<ResourceState> resources)
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
                }),
            Resources = resources.Select(r => new ResourceSnapshot(r.Id, r.Type, r.InUse)).ToList().AsReadOnly()
        };
    }

    private record SimulationResult(
        GameTime Time,
        int HoursElapsed,
        bool GameOver,
        int TotalPassengersSpawned,
        int TotalScore,
        Dictionary<Location, StationState> ActiveStations,
        List<ResourceState> Resources);

    private class StationState(Guid id, StationType type, List<PassengerSpawnPhase> phases, int spawnedOnDay)
    {
        public Guid Id { get; } = id;
        public StationType Type { get; } = type;
        public List<PassengerSpawnPhase> Phases { get; } = phases;
        public int SpawnedOnDay { get; } = spawnedOnDay;
        public int SpawnedAtHour { get; } = (spawnedOnDay - 1) * 24;
        public List<Passenger> Passengers { get; } = [];
    }

    private class ResourceState(Guid id, ResourceType type)
    {
        public Guid Id { get; } = id;
        public ResourceType Type { get; } = type;
        public bool InUse { get; set; }
        public Guid? AssignedToLineId { get; set; }
    }
}