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
        return CreateSnapshot(sim.Time, sim.HoursElapsed, sim.GameOver, sim.ActiveStations, sim.TotalScore, sim.Resources, sim.Lines, sim.Vehicles);
    }

    private SimulationResult RunSimulation(IMetroManiaRunner runner, Level level, int? targetHours = null, CancellationToken cancellationToken = default)
    {
        var random = new Random(level.LevelData.Seed);
        var activeStations = new Dictionary<Location, StationState>();

        // Initialize starting resources: 1 line and 1 vehicle
        var resources = new List<ResourceState>
        {
            new(NextGuid(random), ResourceType.Line),
            new(NextGuid(random), ResourceType.Train)
        };
        var lines = new List<LineState>();
        var vehicles = new List<VehicleState>();

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
                            CreateSnapshot(time, hoursElapsed, gameOver, activeStations, totalScore, resources, lines, vehicles),
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
                        CreateSnapshot(time, hoursElapsed, gameOver, activeStations, totalScore, resources, lines, vehicles),
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
                    var validDestTypes = activeStations.Values
                        .Select(s => s.Type)
                        .Where(t => t != state.Type)
                        .Distinct()
                        .ToArray();

                    if (validDestTypes.Length == 0)
                        continue;

                    var destType = validDestTypes[random.Next(validDestTypes.Length)];

                    state.Passengers.Add(new Passenger(destType));
                    totalPassengersSpawned++;

                    runner.OnPassengerWaiting(
                        CreateSnapshot(time, hoursElapsed, gameOver, activeStations, totalScore, resources, lines, vehicles),
                        location, state.Passengers.AsReadOnly());
                }
            }

            foreach (var (location, state) in activeStations)
            {
                if (state.Passengers.Count >= 20)
                {
                    gameOver = true;
                    runner.OnGameOver(
                        CreateSnapshot(time, hoursElapsed, true, activeStations, totalScore, resources, lines, vehicles),
                        location, state.Passengers.AsReadOnly());
                    break;
                }

                if (state.Passengers.Count >= 10)
                {
                    runner.OnStationOverrun(
                        CreateSnapshot(time, hoursElapsed, gameOver, activeStations, totalScore, resources, lines, vehicles),
                        location, state.Passengers.AsReadOnly());
                }
            }

            if (gameOver) break;

            // Phase 2: OnDayStart — fire second
            if (isDayStart)
            {
                runner.OnDayStart(
                    CreateSnapshot(time, hoursElapsed, gameOver, activeStations, totalScore, resources, lines, vehicles));
            }

            // Move vehicles along their lines before the player acts
            MoveVehicles(vehicles, lines, activeStations);

            // Phase 3: OnHourTick — fire last, then process the player's action
            var action = runner.OnHourTick(
                CreateSnapshot(time, hoursElapsed, gameOver, activeStations, totalScore, resources, lines, vehicles));
            ProcessAction(action, resources, lines, vehicles);
            hoursElapsed++;
        }

        return new SimulationResult(time, hoursElapsed, gameOver, totalPassengersSpawned, totalScore, activeStations, resources, lines, vehicles);
    }

    private static void ProcessAction(PlayerAction action, List<ResourceState> resources, List<LineState> lines, List<VehicleState> vehicles)
    {
        switch (action)
        {
            case CreateLine createLine:
                var lineResource = resources.FirstOrDefault(r => r.Id == createLine.LineId && r.Type == ResourceType.Line && !r.InUse);
                if (lineResource is not null && createLine.StationIds.Count >= 2)
                {
                    lineResource.InUse = true;
                    var line = new LineState(lineResource.Id);
                    line.StationIds.AddRange(createLine.StationIds);
                    lines.Add(line);
                }
                break;

            case RemoveLine removeLine:
                var lineToRemove = lines.FirstOrDefault(l => l.ResourceId == removeLine.LineId);
                if (lineToRemove is not null)
                {
                    var lineRes = resources.FirstOrDefault(r => r.Id == removeLine.LineId);
                    if (lineRes is not null) lineRes.InUse = false;

                    foreach (var v in vehicles.Where(v => v.LineResourceId == removeLine.LineId).ToList())
                    {
                        var vRes = resources.FirstOrDefault(r => r.Id == v.ResourceId);
                        if (vRes is not null) vRes.InUse = false;
                        vehicles.Remove(v);
                    }

                    lines.Remove(lineToRemove);
                }
                break;

            case AddVehicleToLine addVehicle:
                var vehicleResource = resources.FirstOrDefault(r => r.Id == addVehicle.VehicleId && r.Type is ResourceType.Train or ResourceType.Wagon && !r.InUse);
                var targetLine = lines.FirstOrDefault(l => l.ResourceId == addVehicle.LineId);
                if (vehicleResource is not null && targetLine is not null && targetLine.StationIds.Contains(addVehicle.StationId))
                {
                    vehicleResource.InUse = true;
                    int stationIdx = targetLine.StationIds.IndexOf(addVehicle.StationId);
                    int numSegments = targetLine.StationIds.Count - 1;

                    int segIdx;
                    float prog;
                    int dir;
                    if (stationIdx >= numSegments)
                    {
                        segIdx = numSegments - 1;
                        prog = 1.0f;
                        dir = -1;
                    }
                    else
                    {
                        segIdx = stationIdx;
                        prog = 0.0f;
                        dir = 1;
                    }

                    vehicles.Add(new VehicleState(vehicleResource.Id, targetLine.ResourceId, segIdx, prog, dir));
                }
                break;

            case RemoveVehicle removeVehicle:
                var vehicleToRemove = vehicles.FirstOrDefault(v => v.ResourceId == removeVehicle.VehicleId);
                if (vehicleToRemove is not null)
                {
                    var vRes = resources.FirstOrDefault(r => r.Id == removeVehicle.VehicleId);
                    if (vRes is not null) vRes.InUse = false;
                    vehicles.Remove(vehicleToRemove);
                }
                break;

            case ExtendLine extendLine:
                var lineToExtend = lines.FirstOrDefault(l => l.ResourceId == extendLine.LineId);
                if (lineToExtend is not null && lineToExtend.StationIds.Count > 0)
                {
                    if (lineToExtend.StationIds[0] == extendLine.FromStationId)
                        lineToExtend.StationIds.Insert(0, extendLine.ToStationId);
                    else if (lineToExtend.StationIds[^1] == extendLine.FromStationId)
                        lineToExtend.StationIds.Add(extendLine.ToStationId);
                }
                break;

            case InsertStationInLine insertStation:
                var lineToInsert = lines.FirstOrDefault(l => l.ResourceId == insertStation.LineId);
                if (lineToInsert is not null)
                {
                    int fromIdx = lineToInsert.StationIds.IndexOf(insertStation.FromStationId);
                    int toIdx = lineToInsert.StationIds.IndexOf(insertStation.ToStationId);
                    if (fromIdx >= 0 && toIdx >= 0 && Math.Abs(fromIdx - toIdx) == 1)
                    {
                        int insertIdx = Math.Max(fromIdx, toIdx);
                        lineToInsert.StationIds.Insert(insertIdx, insertStation.NewStationId);
                    }
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
        List<ResourceState> resources, List<LineState> lines, List<VehicleState> vehicles)
    {
        var snapshot = new GameSnapshot
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
            Resources = resources.Select(r => new ResourceSnapshot(r.Id, r.Type, r.InUse)).ToList().AsReadOnly(),
            Lines = lines.Select(l => new LineSnapshot
            {
                LineId = l.ResourceId,
                StationIds = l.StationIds.ToList().AsReadOnly()
            }).ToList().AsReadOnly(),
            Vehicles = vehicles.Select(v =>
            {
                var line = lines.FirstOrDefault(l => l.ResourceId == v.LineResourceId);
                Guid? stationId = null;
                if (line is not null)
                {
                    if (v.Progress <= 0.0f)
                        stationId = line.StationIds[v.SegmentIndex];
                    else if (v.Progress >= 1.0f)
                        stationId = line.StationIds[v.SegmentIndex + 1];
                }
                return new VehicleSnapshot
                {
                    VehicleId = v.ResourceId,
                    LineId = v.LineResourceId,
                    SegmentIndex = v.SegmentIndex,
                    Progress = v.Direction == -1 ? 1.0f - v.Progress : v.Progress,
                    Direction = v.Direction,
                    StationId = stationId
                };
            }).ToList().AsReadOnly()
        };

        foreach (var station in snapshot.Stations.Values)
            station.Snapshot = snapshot;
        foreach (var line in snapshot.Lines)
            line.Snapshot = snapshot;
        foreach (var vehicle in snapshot.Vehicles)
            vehicle.Snapshot = snapshot;

        return snapshot;
    }

    private record SimulationResult(
        GameTime Time,
        int HoursElapsed,
        bool GameOver,
        int TotalPassengersSpawned,
        int TotalScore,
        Dictionary<Location, StationState> ActiveStations,
        List<ResourceState> Resources,
        List<LineState> Lines,
        List<VehicleState> Vehicles);

    private static void MoveVehicles(
        List<VehicleState> vehicles, List<LineState> lines,
        Dictionary<Location, StationState> activeStations, float speedPerHour = 1.0f)
    {
        var stationLocations = activeStations.ToDictionary(kvp => kvp.Value.Id, kvp => kvp.Key);

        foreach (var vehicle in vehicles)
        {
            var line = lines.FirstOrDefault(l => l.ResourceId == vehicle.LineResourceId);
            if (line is null || line.StationIds.Count < 2)
                continue;

            int numSegments = line.StationIds.Count - 1;
            float remainingSpeed = speedPerHour;

            while (remainingSpeed > 0.001f)
            {
                if (!stationLocations.TryGetValue(line.StationIds[vehicle.SegmentIndex], out var fromLoc) ||
                    !stationLocations.TryGetValue(line.StationIds[vehicle.SegmentIndex + 1], out var toLoc))
                    break;

                float segmentLength = Distance(fromLoc, toLoc);
                if (segmentLength < 0.001f)
                    break;

                float progressDelta = remainingSpeed / segmentLength;

                if (vehicle.Direction == 1)
                {
                    float newProgress = vehicle.Progress + progressDelta;
                    if (newProgress < 1.0f)
                    {
                        vehicle.Progress = newProgress;
                        remainingSpeed = 0;
                    }
                    else
                    {
                        remainingSpeed = (newProgress - 1.0f) * segmentLength;
                        if (vehicle.SegmentIndex < numSegments - 1)
                        {
                            vehicle.SegmentIndex++;
                            vehicle.Progress = 0.0f;
                        }
                        else
                        {
                            vehicle.Progress = 1.0f;
                            vehicle.Direction = -1;
                        }
                    }
                }
                else
                {
                    float newProgress = vehicle.Progress - progressDelta;
                    if (newProgress > 0.0f)
                    {
                        vehicle.Progress = newProgress;
                        remainingSpeed = 0;
                    }
                    else
                    {
                        remainingSpeed = (0.0f - newProgress) * segmentLength;
                        if (vehicle.SegmentIndex > 0)
                        {
                            vehicle.SegmentIndex--;
                            vehicle.Progress = 1.0f;
                        }
                        else
                        {
                            vehicle.Progress = 0.0f;
                            vehicle.Direction = 1;
                        }
                    }
                }
            }
        }
    }

    private static float Distance(Location a, Location b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

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
    }

    private class LineState(Guid resourceId)
    {
        public Guid ResourceId { get; } = resourceId;
        public List<Guid> StationIds { get; } = [];
    }

    private class VehicleState(Guid resourceId, Guid lineResourceId, int segmentIndex, float progress, int direction)
    {
        public Guid ResourceId { get; } = resourceId;
        public Guid LineResourceId { get; } = lineResourceId;
        public int SegmentIndex { get; set; } = segmentIndex;
        public float Progress { get; set; } = progress;
        public int Direction { get; set; } = direction;
    }
}