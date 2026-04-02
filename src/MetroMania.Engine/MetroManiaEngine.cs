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
            Score = sim.TotalScore,
            TimeTaken = stopwatch.Elapsed,
            DaysSurvived = sim.Time.Day,
            TotalPassengersSpawned = sim.TotalPassengersSpawned,
            DebugInfo = new GameDebugInfo(level.LevelData, sim.HourlySnapshots)
        };
    }

    /// <summary>
    /// Runs the game simulation for a specific number of hours and returns a snapshot of the state.
    /// If the game ends before reaching the target hours, the snapshot reflects the game-over state.
    /// </summary>
    public GameSnapshot RunForHours(IMetroManiaRunner runner, Level level, int targetHours, CancellationToken cancellationToken = default)
    {
        var sim = RunSimulation(runner, level, targetHours, cancellationToken);
        return CreateSnapshot(sim.Time, sim.HoursElapsed, sim.GameOver, sim.ActiveStations, sim.TotalScore, sim.Resources, sim.Lines, sim.Vehicles, sim.VehicleCapacity);
    }

    /// <summary>
    /// Runs the full game simulation and returns one snapshot per hour elapsed.
    /// Each snapshot reflects the state at the end of that hour tick.
    /// This is efficient: the simulation runs exactly once.
    /// </summary>
    public IReadOnlyList<GameSnapshot> RunWithHourlySnapshots(IMetroManiaRunner runner, Level level, CancellationToken cancellationToken = default)
    {
        var random = new Random(level.LevelData.Seed);
        var vehicleCapacity = level.LevelData.VehicleCapacity;
        var maxDays = level.LevelData.MaxDays;
        var activeStations = new Dictionary<Location, StationState>();

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
        var snapshots = new List<GameSnapshot>();

        while (!gameOver && (maxDays <= 0 || hoursElapsed < maxDays * 24))
        {
            cancellationToken.ThrowIfCancellationRequested();
            int day = hoursElapsed / 24 + 1;
            int currentHour = hoursElapsed % 24;
            var dayOfWeek = (DayOfWeek)(day % 7);
            time = new GameTime(day, currentHour, dayOfWeek);
            bool isDayStart = currentHour == 0;

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
                            CreateSnapshot(time, hoursElapsed, gameOver, activeStations, totalScore, resources, lines, vehicles, vehicleCapacity),
                            stationId, location, spawn.StationType);
                    }
                }

                if (dayOfWeek == DayOfWeek.Monday)
                {
                    int weekNumber = (day - 1) / 7 + 1;
                    var giftOverride = level.LevelData.WeeklyGiftOverrides
                        .FirstOrDefault(g => g.Week == weekNumber);
                    var gift = giftOverride is not null ? giftOverride.ResourceType : (ResourceType)random.Next(3);
                    resources.Add(new ResourceState(NextGuid(random), gift));
                    runner.OnWeeklyGift(
                        CreateSnapshot(time, hoursElapsed, gameOver, activeStations, totalScore, resources, lines, vehicles, vehicleCapacity),
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

                if (activePhase is null) continue;

                if ((totalHour - state.SpawnedAtHour) % activePhase.FrequencyInHours == 0
                    && totalHour > state.SpawnedAtHour)
                {
                    var validDestTypes = activeStations.Values
                        .Select(s => s.Type)
                        .Where(t => t != state.Type)
                        .Distinct()
                        .ToArray();
                    if (validDestTypes.Length == 0) continue;
                    var destType = validDestTypes[random.Next(validDestTypes.Length)];
                    state.Passengers.Add(new Passenger(destType, totalHour));
                    totalPassengersSpawned++;
                    runner.OnPassengerWaiting(
                        CreateSnapshot(time, hoursElapsed, gameOver, activeStations, totalScore, resources, lines, vehicles, vehicleCapacity),
                        location, state.Passengers.AsReadOnly());
                }
            }

            foreach (var (location, state) in activeStations)
            {
                if (state.Passengers.Count >= 20)
                {
                    gameOver = true;
                    runner.OnGameOver(
                        CreateSnapshot(time, hoursElapsed, true, activeStations, totalScore, resources, lines, vehicles, vehicleCapacity),
                        location, state.Passengers.AsReadOnly());
                    break;
                }
                if (state.Passengers.Count >= 10)
                {
                    runner.OnStationOverrun(
                        CreateSnapshot(time, hoursElapsed, gameOver, activeStations, totalScore, resources, lines, vehicles, vehicleCapacity),
                        location, state.Passengers.AsReadOnly());
                }
            }

            if (gameOver) break;

            if (isDayStart)
            {
                runner.OnDayStart(
                    CreateSnapshot(time, hoursElapsed, gameOver, activeStations, totalScore, resources, lines, vehicles, vehicleCapacity));
            }

            totalScore += ProcessStationStops(vehicles, lines, activeStations, vehicleCapacity, totalHour);
            MoveVehicles(vehicles, lines, activeStations);

            var action = runner.OnHourTick(
                CreateSnapshot(time, hoursElapsed, gameOver, activeStations, totalScore, resources, lines, vehicles, vehicleCapacity));
            ProcessAction(action, resources, lines, vehicles, activeStations);
            hoursElapsed++;

            snapshots.Add(CreateSnapshot(time, hoursElapsed, gameOver, activeStations, totalScore, resources, lines, vehicles, vehicleCapacity, action));
        }

        return snapshots;
    }

    private SimulationResult RunSimulation(IMetroManiaRunner runner, Level level, int? targetHours = null, CancellationToken cancellationToken = default)
    {
        var random = new Random(level.LevelData.Seed);
        var vehicleCapacity = level.LevelData.VehicleCapacity;
        var maxDays = level.LevelData.MaxDays;
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
        var hourlySnapshots = new List<GameSnapshot>();

        while (!gameOver && (targetHours is null || hoursElapsed < targetHours) && (maxDays <= 0 || hoursElapsed < maxDays * 24))
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
                            CreateSnapshot(time, hoursElapsed, gameOver, activeStations, totalScore, resources, lines, vehicles, vehicleCapacity),
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
                        CreateSnapshot(time, hoursElapsed, gameOver, activeStations, totalScore, resources, lines, vehicles, vehicleCapacity),
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

                    state.Passengers.Add(new Passenger(destType, totalHour));
                    totalPassengersSpawned++;

                    runner.OnPassengerWaiting(
                        CreateSnapshot(time, hoursElapsed, gameOver, activeStations, totalScore, resources, lines, vehicles, vehicleCapacity),
                        location, state.Passengers.AsReadOnly());
                }
            }

            foreach (var (location, state) in activeStations)
            {
                if (state.Passengers.Count >= 20)
                {
                    gameOver = true;
                    runner.OnGameOver(
                        CreateSnapshot(time, hoursElapsed, true, activeStations, totalScore, resources, lines, vehicles, vehicleCapacity),
                        location, state.Passengers.AsReadOnly());
                    break;
                }

                if (state.Passengers.Count >= 10)
                {
                    runner.OnStationOverrun(
                        CreateSnapshot(time, hoursElapsed, gameOver, activeStations, totalScore, resources, lines, vehicles, vehicleCapacity),
                        location, state.Passengers.AsReadOnly());
                }
            }

            if (gameOver) break;

            // Phase 2: OnDayStart — fire second
            if (isDayStart)
            {
                runner.OnDayStart(
                    CreateSnapshot(time, hoursElapsed, gameOver, activeStations, totalScore, resources, lines, vehicles, vehicleCapacity));
            }

            // Process passenger pickup and dropoff at stations (before moving)
            totalScore += ProcessStationStops(vehicles, lines, activeStations, vehicleCapacity, totalHour);

            // Move vehicles along their lines before the player acts
            MoveVehicles(vehicles, lines, activeStations);

            // Phase 3: OnHourTick — fire last, then process the player's action
            var tickSnapshot = CreateSnapshot(time, hoursElapsed, gameOver, activeStations, totalScore, resources, lines, vehicles, vehicleCapacity);
            var action = runner.OnHourTick(tickSnapshot);
            hourlySnapshots.Add(tickSnapshot);
            ProcessAction(action, resources, lines, vehicles, activeStations);
            hoursElapsed++;
        }

        return new SimulationResult(time, hoursElapsed, gameOver, totalPassengersSpawned, totalScore, activeStations, resources, lines, vehicles, vehicleCapacity, hourlySnapshots);
    }

    private static void ProcessAction(PlayerAction action, List<ResourceState> resources, List<LineState> lines, List<VehicleState> vehicles, Dictionary<Location, StationState> activeStations)
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
                        // Release attached wagons
                        foreach (var wagonId in v.WagonIds)
                        {
                            var wRes = resources.FirstOrDefault(r => r.Id == wagonId);
                            if (wRes is not null) wRes.InUse = false;
                        }

                        var vRes = resources.FirstOrDefault(r => r.Id == v.ResourceId);
                        if (vRes is not null) vRes.InUse = false;
                        vehicles.Remove(v);
                    }

                    lines.Remove(lineToRemove);
                }
                break;

            case AddVehicleToLine addVehicle:
                var vehicleResource = resources.FirstOrDefault(r => r.Id == addVehicle.VehicleId && r.Type == ResourceType.Train && !r.InUse);
                var targetLine = lines.FirstOrDefault(l => l.ResourceId == addVehicle.LineId);
                if (vehicleResource is not null && targetLine is not null && targetLine.StationIds.Contains(addVehicle.StationId))
                {
                    vehicleResource.InUse = true;
                    int stationIdx = targetLine.StationIds.IndexOf(addVehicle.StationId);
                    int numSegments = targetLine.StationIds.Count - 1;
                    var stationLocations = activeStations.ToDictionary(kvp => kvp.Value.Id, kvp => kvp.Key);

                    int segIdx;
                    int tileOffset;
                    int dir;
                    if (stationIdx >= numSegments)
                    {
                        segIdx = numSegments - 1;
                        tileOffset = stationLocations.TryGetValue(targetLine.StationIds[segIdx], out var aFrom) &&
                                     stationLocations.TryGetValue(targetLine.StationIds[segIdx + 1], out var aTo)
                            ? Distance(aFrom, aTo) : 0;
                        dir = -1;
                    }
                    else
                    {
                        segIdx = stationIdx;
                        tileOffset = 0;
                        dir = 1;
                    }

                    vehicles.Add(new VehicleState(vehicleResource.Id, targetLine.ResourceId, segIdx, tileOffset, dir));
                }
                break;

            case RemoveVehicle removeVehicle:
                var vehicleToRemove = vehicles.FirstOrDefault(v => v.ResourceId == removeVehicle.VehicleId);
                if (vehicleToRemove is not null)
                {
                    // Release attached wagons
                    foreach (var wagonId in vehicleToRemove.WagonIds)
                    {
                        var wr = resources.FirstOrDefault(r => r.Id == wagonId);
                        if (wr is not null) wr.InUse = false;
                    }
                    vehicleToRemove.WagonIds.Clear();

                    var vRes = resources.FirstOrDefault(r => r.Id == removeVehicle.VehicleId);
                    if (vRes is not null) vRes.InUse = false;
                    vehicles.Remove(vehicleToRemove);
                }
                break;

            case ExtendLine extendLine:
                var lineToExtend = lines.FirstOrDefault(l => l.ResourceId == extendLine.LineId);
                if (lineToExtend is not null && lineToExtend.StationIds.Count > 0
                    && extendLine.FromStationId != extendLine.ToStationId
                    && !lineToExtend.StationIds.Contains(extendLine.ToStationId))
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

            case AddWagonToTrain addWagon:
                var wagonResource = resources.FirstOrDefault(r => r.Id == addWagon.WagonId && r.Type == ResourceType.Wagon && !r.InUse);
                var trainVehicle = vehicles.FirstOrDefault(v => v.ResourceId == addWagon.TrainId);
                var trainResource = trainVehicle is not null ? resources.FirstOrDefault(r => r.Id == addWagon.TrainId && r.Type == ResourceType.Train && r.InUse) : null;
                if (wagonResource is not null && trainVehicle is not null && trainResource is not null)
                {
                    wagonResource.InUse = true;
                    trainVehicle.WagonIds.Add(wagonResource.Id);
                }
                break;

            case MoveWagonBetweenTrains moveWagon:
                var sourceTrain = vehicles.FirstOrDefault(v => v.ResourceId == moveWagon.SourceTrainId);
                var destTrain = vehicles.FirstOrDefault(v => v.ResourceId == moveWagon.DestinationTrainId);
                var wagonRes = resources.FirstOrDefault(r => r.Id == moveWagon.WagonId && r.Type == ResourceType.Wagon && r.InUse);
                if (sourceTrain is not null && destTrain is not null && wagonRes is not null
                    && sourceTrain.WagonIds.Contains(moveWagon.WagonId))
                {
                    sourceTrain.WagonIds.Remove(moveWagon.WagonId);
                    destTrain.WagonIds.Add(moveWagon.WagonId);
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
        List<ResourceState> resources, List<LineState> lines, List<VehicleState> vehicles,
        int vehicleCapacity, PlayerAction? lastAction = null)
    {
        var stationLocationsForSnap = activeStations.ToDictionary(kvp => kvp.Value.Id, kvp => kvp.Key);
        var snapshot = new GameSnapshot
        {
            Time = time,
            TotalHoursElapsed = hoursElapsed,
            GameOver = gameOver,
            TotalScore = totalScore,
            LastAction = lastAction,
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
                decimal progress = 0m;
                if (line is not null)
                {
                    if (v.TileOffset == 0)
                    {
                        stationId = line.StationIds[v.SegmentIndex];
                        progress = 0m;
                    }
                    else if (stationLocationsForSnap.TryGetValue(line.StationIds[v.SegmentIndex], out var sFrom) &&
                             stationLocationsForSnap.TryGetValue(line.StationIds[v.SegmentIndex + 1], out var sTo))
                    {
                        int segLen = Distance(sFrom, sTo);
                        if (v.TileOffset >= segLen)
                        {
                            stationId = line.StationIds[v.SegmentIndex + 1];
                            progress = 1m;
                        }
                        else
                        {
                            progress = segLen > 0 ? (decimal)v.TileOffset / segLen : 0m;
                        }
                    }
                }
                return new VehicleSnapshot
                {
                    VehicleId = v.ResourceId,
                    LineId = v.LineResourceId,
                    SegmentIndex = v.SegmentIndex,
                    Progress = progress,
                    Direction = v.Direction,
                    StationId = stationId,
                    Capacity = vehicleCapacity,
                    WagonIds = v.WagonIds.ToList().AsReadOnly(),
                    Passengers = v.Passengers.ToList().AsReadOnly()
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
        List<VehicleState> Vehicles,
        int VehicleCapacity,
        List<GameSnapshot> HourlySnapshots);

    private static void MoveVehicles(
        List<VehicleState> vehicles, List<LineState> lines,
        Dictionary<Location, StationState> activeStations)
    {
        var stationLocations = activeStations.ToDictionary(kvp => kvp.Value.Id, kvp => kvp.Key);

        foreach (var vehicle in vehicles)
        {
            if (vehicle.DwellTicksRemaining > 0)
            {
                vehicle.DwellTicksRemaining--;
                continue;
            }

            var line = lines.FirstOrDefault(l => l.ResourceId == vehicle.LineResourceId);
            if (line is null || line.StationIds.Count < 2) continue;

            int numSegments = line.StationIds.Count - 1;

            if (!stationLocations.TryGetValue(line.StationIds[vehicle.SegmentIndex], out var fromLoc) ||
                !stationLocations.TryGetValue(line.StationIds[vehicle.SegmentIndex + 1], out var toLoc))
                continue;

            int segLen = Distance(fromLoc, toLoc);
            if (segLen == 0) continue;

            vehicle.TileOffset += vehicle.Direction;

            if (vehicle.Direction == 1 && vehicle.TileOffset >= segLen)
            {
                if (vehicle.SegmentIndex < numSegments - 1)
                {
                    vehicle.SegmentIndex++;
                    vehicle.TileOffset = 0;
                }
                else
                {
                    vehicle.TileOffset = segLen;
                    vehicle.Direction = -1;
                }
            }
            else if (vehicle.Direction == -1 && vehicle.TileOffset <= 0)
            {
                if (vehicle.SegmentIndex > 0)
                {
                    vehicle.SegmentIndex--;
                    if (stationLocations.TryGetValue(line.StationIds[vehicle.SegmentIndex], out var prevFrom) &&
                        stationLocations.TryGetValue(line.StationIds[vehicle.SegmentIndex + 1], out var prevTo))
                        vehicle.TileOffset = Distance(prevFrom, prevTo);
                    else
                        vehicle.TileOffset = 0;
                }
                else
                {
                    vehicle.TileOffset = 0;
                    vehicle.Direction = 1;
                }
            }
        }
    }

    /// <summary>
    /// Handles passenger dropoff and pickup when vehicles are at stations.
    /// Returns the number of passengers successfully delivered (score increase).
    /// </summary>
    private static int ProcessStationStops(
        List<VehicleState> vehicles, List<LineState> lines,
        Dictionary<Location, StationState> activeStations, int vehicleCapacity, int currentHour)
    {
        int delivered = 0;
        var stationById = activeStations.ToDictionary(kvp => kvp.Value.Id, kvp => kvp);

        foreach (var vehicle in vehicles)
        {
            var line = lines.FirstOrDefault(l => l.ResourceId == vehicle.LineResourceId);
            if (line is null) continue;

            Guid? currentStationId = null;
            if (vehicle.TileOffset == 0)
            {
                currentStationId = line.StationIds[vehicle.SegmentIndex];
            }
            else
            {
                if (stationById.TryGetValue(line.StationIds[vehicle.SegmentIndex], out var fromEntry) &&
                    stationById.TryGetValue(line.StationIds[vehicle.SegmentIndex + 1], out var toEntry))
                {
                    int segLen = Distance(fromEntry.Key, toEntry.Key);
                    if (vehicle.TileOffset == segLen)
                        currentStationId = line.StationIds[vehicle.SegmentIndex + 1];
                }
            }

            if (currentStationId is null) continue;
            if (!stationById.TryGetValue(currentStationId.Value, out var stationEntry)) continue;

            var station = stationEntry.Value;
            int totalCapacity = vehicleCapacity * (1 + vehicle.WagonIds.Count);

            // Phase 1: Drop off one passenger whose destination matches this station type.
            // Drop-offs always take priority over pick-ups; only one action per tick.
            var toDropOff = vehicle.Passengers.FirstOrDefault(p => p.DestinationType == station.Type);
            if (toDropOff is not null)
            {
                vehicle.Passengers.Remove(toDropOff);
                delivered++;
                vehicle.DwellTicksRemaining = 1;
                continue;
            }

            // Phase 2: Pick up one eligible passenger (spawned before this hour, first-spawned first).
            if (vehicle.Passengers.Count < totalCapacity)
            {
                var eligible = station.Passengers
                    .Where(p => p.SpawnedAtHour < currentHour)
                    .ToList();

                foreach (var passenger in eligible)
                {
                    var closestViaNetwork = FindClosestStationOfType(
                        currentStationId.Value, passenger.DestinationType,
                        lines, stationById, activeStations);
                    if (closestViaNetwork is null) continue;

                    var closestViaThisLine = FindClosestStationOfTypeOnLine(
                        currentStationId.Value, passenger.DestinationType,
                        vehicle.Direction, vehicle.SegmentIndex, vehicle.TileOffset,
                        line, stationById, activeStations);
                    if (closestViaThisLine is null) continue;
                    if (closestViaNetwork.Value.distance < closestViaThisLine.Value.distance) continue;

                    station.Passengers.Remove(passenger);
                    vehicle.Passengers.Add(passenger);
                    vehicle.DwellTicksRemaining = 1;
                    break; // One pick-up per tick
                }
            }
        }

        return delivered;
    }

    /// <summary>
    /// Returns all station IDs reachable from a starting station by traversing connected lines.
    /// Stations connected via shared stops (transfer stations) are included.
    /// </summary>
    private static HashSet<Guid> GetReachableStations(
        Guid startStationId, List<LineState> lines,
        Dictionary<Guid, KeyValuePair<Location, StationState>> stationById)
    {
        var visited = new HashSet<Guid> { startStationId };
        var queue = new Queue<Guid>();
        queue.Enqueue(startStationId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var line in lines)
            {
                if (!line.StationIds.Contains(current)) continue;
                foreach (var sid in line.StationIds)
                {
                    if (visited.Add(sid))
                        queue.Enqueue(sid);
                }
            }
        }

        return visited;
    }

    /// <summary>
    /// Finds the closest station of the given type reachable via the entire network (BFS by distance).
    /// </summary>
    private static (Guid stationId, int distance)? FindClosestStationOfType(
        Guid fromStationId, StationType targetType,
        List<LineState> lines,
        Dictionary<Guid, KeyValuePair<Location, StationState>> stationById,
        Dictionary<Location, StationState> activeStations)
    {
        var graph = BuildStationGraph(lines, stationById, activeStations);

        var dist = new Dictionary<Guid, int> { [fromStationId] = 0 };
        var pq = new PriorityQueue<Guid, int>();
        pq.Enqueue(fromStationId, 0);

        (Guid stationId, int distance)? best = null;

        while (pq.Count > 0)
        {
            var current = pq.Dequeue();
            int currentDist = dist[current];

            if (best.HasValue && currentDist >= best.Value.distance) break;

            if (current != fromStationId && stationById.TryGetValue(current, out var entry) && entry.Value.Type == targetType)
            {
                if (!best.HasValue || currentDist < best.Value.distance)
                    best = (current, currentDist);
                continue;
            }

            if (!graph.TryGetValue(current, out var neighbors)) continue;
            foreach (var (neighbor, edgeDist) in neighbors)
            {
                int newDist = currentDist + edgeDist;
                if (!dist.ContainsKey(neighbor) || newDist < dist[neighbor])
                {
                    dist[neighbor] = newDist;
                    pq.Enqueue(neighbor, newDist);
                }
            }
        }

        return best;
    }

    /// <summary>
    /// Finds the closest station of the given type that the vehicle will reach on its current line,
    /// following its current direction (and future ping-pong path).
    /// Returns the travel distance along the line to that station.
    /// </summary>
    private static (Guid stationId, int distance)? FindClosestStationOfTypeOnLine(
        Guid currentStationId, StationType targetType,
        int direction, int segmentIndex, int tileOffset,
        LineState line,
        Dictionary<Guid, KeyValuePair<Location, StationState>> stationById,
        Dictionary<Location, StationState> activeStations)
    {
        // Collect all stations on this line and their cumulative distances from station[0]
        var cumulativeDist = new int[line.StationIds.Count];
        cumulativeDist[0] = 0;
        for (int i = 1; i < line.StationIds.Count; i++)
        {
            if (stationById.TryGetValue(line.StationIds[i - 1], out var prev) &&
                stationById.TryGetValue(line.StationIds[i], out var curr))
            {
                cumulativeDist[i] = cumulativeDist[i - 1] + Distance(prev.Key, curr.Key);
            }
        }

        // Current position as cumulative distance from station[0]
        int currentPos = cumulativeDist[segmentIndex] + tileOffset;

        // Search in the direction of travel first, then the other direction (ping-pong)
        int bestDist = int.MaxValue;
        Guid? bestStation = null;

        // Forward pass (in current direction)
        if (direction == 1)
        {
            // Check stations ahead (higher index)
            for (int i = 0; i < line.StationIds.Count; i++)
            {
                if (cumulativeDist[i] <= currentPos) continue;
                if (stationById.TryGetValue(line.StationIds[i], out var entry) && entry.Value.Type == targetType)
                {
                    int d = cumulativeDist[i] - currentPos;
                    if (d < bestDist) { bestDist = d; bestStation = line.StationIds[i]; }
                    break;
                }
            }
            // Also check ping-pong: stations behind (after reaching end and coming back)
            if (!bestStation.HasValue)
            {
                int totalLength = cumulativeDist[^1];
                int distToEnd = totalLength - currentPos;
                for (int i = line.StationIds.Count - 2; i >= 0; i--)
                {
                    if (stationById.TryGetValue(line.StationIds[i], out var entry) && entry.Value.Type == targetType)
                    {
                        int d = distToEnd + (totalLength - cumulativeDist[i]);
                        if (d < bestDist) { bestDist = d; bestStation = line.StationIds[i]; }
                        break;
                    }
                }
            }
        }
        else
        {
            // Check stations behind (lower index)
            for (int i = line.StationIds.Count - 1; i >= 0; i--)
            {
                if (cumulativeDist[i] >= currentPos) continue;
                if (stationById.TryGetValue(line.StationIds[i], out var entry) && entry.Value.Type == targetType)
                {
                    int d = currentPos - cumulativeDist[i];
                    if (d < bestDist) { bestDist = d; bestStation = line.StationIds[i]; }
                    break;
                }
            }
            // Ping-pong: reach start, then forward
            if (!bestStation.HasValue)
            {
                int distToStart = currentPos;
                for (int i = 1; i < line.StationIds.Count; i++)
                {
                    if (stationById.TryGetValue(line.StationIds[i], out var entry) && entry.Value.Type == targetType)
                    {
                        int d = distToStart + cumulativeDist[i];
                        if (d < bestDist) { bestDist = d; bestStation = line.StationIds[i]; }
                        break;
                    }
                }
            }
        }

        return bestStation.HasValue ? (bestStation.Value, bestDist) : null;
    }

    /// <summary>
    /// Builds an adjacency graph of station connections with Chebyshev distances.
    /// </summary>
    private static Dictionary<Guid, List<(Guid neighbor, int distance)>> BuildStationGraph(
        List<LineState> lines,
        Dictionary<Guid, KeyValuePair<Location, StationState>> stationById,
        Dictionary<Location, StationState> activeStations)
    {
        var graph = new Dictionary<Guid, List<(Guid, int)>>();

        foreach (var line in lines)
        {
            for (int i = 0; i < line.StationIds.Count - 1; i++)
            {
                var a = line.StationIds[i];
                var b = line.StationIds[i + 1];

                if (stationById.TryGetValue(a, out var entryA) && stationById.TryGetValue(b, out var entryB))
                {
                    int d = Distance(entryA.Key, entryB.Key);

                    if (!graph.ContainsKey(a)) graph[a] = [];
                    if (!graph.ContainsKey(b)) graph[b] = [];
                    graph[a].Add((b, d));
                    graph[b].Add((a, d));
                }
            }
        }

        return graph;
    }

    private static int Distance(Location a, Location b)
        => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

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

    private class VehicleState(Guid resourceId, Guid lineResourceId, int segmentIndex, int tileOffset, int direction)
    {
        public Guid ResourceId { get; } = resourceId;
        public Guid LineResourceId { get; } = lineResourceId;
        public int SegmentIndex { get; set; } = segmentIndex;
        public int TileOffset { get; set; } = tileOffset;
        public int Direction { get; set; } = direction;
        public List<Guid> WagonIds { get; } = [];
        public List<Passenger> Passengers { get; } = [];
        public int DwellTicksRemaining { get; set; }
    }
}