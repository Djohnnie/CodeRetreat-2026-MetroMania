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
    /// Per-tick decision for a single train, computed during Phase 1 of <see cref="ProcessTrains"/>
    /// and potentially revised during Phase 2 collision resolution.
    /// </summary>
    private record struct TrainTick(
        /// <summary>True when the train is staying at a station to perform a passenger operation.</summary>
        bool HasWork,
        /// <summary>Passenger to drop off this tick; null if none.</summary>
        Passenger? DropOff,
        /// <summary>Passenger to pick up this tick; null if none.</summary>
        Passenger? PickUp,
        /// <summary>Tile the train will occupy after this tick (its current tile when blocked or working).</summary>
        Location FinalTile,
        /// <summary>Travel direction the train carries into the next tick.</summary>
        int FinalDirection
    );

    /// <summary>
    /// Processes all trains for one simulation tick using a three-phase pipeline:
    ///   Phase 1 – Each train independently decides what it WANTS to do (work or move).
    ///             All reads use the STARTING snapshot so decisions are simultaneous.
    ///   Phase 2 – Collision resolution: blocking is propagated iteratively to a
    ///             fixed point so that newly blocked trains can themselves block
    ///             trains behind them.
    ///   Phase 3 – Results are applied: passenger operations execute and trains move.
    ///
    /// Collision rules enforced:
    ///   A. Station occupation (direction-agnostic): only one train at a station tile at a time.
    ///      A train that would enter an occupied station is held at its current tile.
    ///   B. Non-station same-direction: a blocked (held) train on open track prevents
    ///      trains behind it (same direction) from advancing into its tile.
    ///      Trains going in the opposite direction may cross freely.
    ///   C. Simultaneous station arrival: when two moving trains target the same station
    ///      in the same tick the lower-index train wins; the other is blocked.
    /// </summary>
    private static GameSnapshot ProcessTrains(Level level, GameSnapshot snapshot)
    {
        if (snapshot.Trains.Count == 0)
            return snapshot;

        var stationLocations = snapshot.Stations
            .ToDictionary(kvp => kvp.Value.Id, kvp => kvp.Key);
        var stationTiles = new HashSet<Location>(snapshot.Stations.Keys);

        // Work on mutable copies; committed in a single with-expression at the end.
        var trains = snapshot.Trains.ToList();
        var waitingPassengers = snapshot.Passengers.ToList();
        int pointsScored = 0;

        // ── Precompute tile paths once – reused across all three phases ────────────
        var tilePaths = trains
            .Select(t =>
            {
                var line = snapshot.Lines.FirstOrDefault(l => l.LineId == t.LineId);
                return line is null ? (List<Location>)[] : LinePathHelper.ComputeTilePath(line, stationLocations);
            })
            .ToArray();

        // ══════════════════════════════════════════════════════════════════════════
        // PHASE 1: Compute desired action for every train independently.
        // ══════════════════════════════════════════════════════════════════════════
        var ticks = new TrainTick[trains.Count];

        for (int t = 0; t < trains.Count; t++)
        {
            var train = trains[t];
            var tilePath = tilePaths[t];

            if (tilePath.Count == 0)
            {
                // Line has no computable path yet — train idles in place.
                ticks[t] = new TrainTick(false, null, null, train.TilePosition, train.Direction);
                continue;
            }

            // ── Train is at a station: check for passenger work ────────────────
            if (snapshot.Stations.TryGetValue(train.TilePosition, out var currentStation))
            {
                // 1. Drop off a passenger whose destination type matches this station.
                var toDrop = train.Passengers.FirstOrDefault(p => p.DestinationType == currentStation.StationType);
                if (toDrop is not null)
                {
                    ticks[t] = new TrainTick(true, toDrop, null, train.TilePosition, train.Direction);
                    continue;
                }

                // 2. Pick up the oldest waiting passenger (FIFO) whose destination is
                //    reachable in the outgoing travel direction.
                //    At a terminal, the next move flips direction, so we look ahead
                //    using the post-flip direction instead of the stored one.
                int currentIndex = tilePath.IndexOf(train.TilePosition);
                bool wouldStepOffPath = currentIndex + train.Direction < 0
                                     || currentIndex + train.Direction >= tilePath.Count;
                int effectiveDir = wouldStepOffPath ? -train.Direction : train.Direction;
                var futureTypes = GetFutureStationTypes(tilePath, currentIndex, effectiveDir, snapshot.Stations);

                var toPickUp = waitingPassengers
                    .Where(p =>
                        p.StationId == currentStation.Id &&
                        train.Passengers.Count < level.LevelData.VehicleCapacity &&
                        futureTypes.Contains(p.DestinationType))
                    .MinBy(p => p.SpawnedAtHour);

                if (toPickUp is not null)
                {
                    ticks[t] = new TrainTick(true, null, toPickUp, train.TilePosition, train.Direction);
                    continue;
                }
            }

            // ── No work at this tick — compute movement ────────────────────────
            if (tilePath.Count < 2)
            {
                // Single-tile path; train cannot move.
                ticks[t] = new TrainTick(false, null, null, train.TilePosition, train.Direction);
                continue;
            }

            int idx = tilePath.IndexOf(train.TilePosition);
            if (idx == -1)
            {
                // Position is not on the current path (e.g. path shrank) — snap to start.
                ticks[t] = new TrainTick(false, null, null, tilePath[0], 1);
                continue;
            }

            int dir = train.Direction;
            int nextIdx = idx + dir;

            // Reverse direction when reaching either terminal of the line.
            if (nextIdx < 0 || nextIdx >= tilePath.Count)
            {
                dir = -dir;
                nextIdx = idx + dir;
            }
            nextIdx = Math.Clamp(nextIdx, 0, tilePath.Count - 1);

            ticks[t] = new TrainTick(false, null, null, tilePath[nextIdx], dir);
        }

        // ══════════════════════════════════════════════════════════════════════════
        // PHASE 2: Collision resolution — iterate to a fixed point.
        //
        // Each pass rebuilds the occupied-tile sets from scratch so that trains
        // newly blocked in the current pass can cascade their blocking to trains
        // behind them in subsequent passes.
        // ══════════════════════════════════════════════════════════════════════════
        bool anyChange;
        do
        {
            anyChange = false;

            // ── Build the current occupied-tile sets from all staying trains ────
            // A staying train is one whose FinalTile equals its starting position
            // (it is either working at a station or was blocked in a previous pass).
            var occupiedStations = new HashSet<Location>();
            var occupiedNonStationByDir = new HashSet<(Location Tile, int Dir)>();

            for (int t = 0; t < trains.Count; t++)
            {
                var tick = ticks[t];
                bool isStaying = tick.FinalTile == trains[t].TilePosition;
                if (!isStaying) continue;

                if (stationTiles.Contains(tick.FinalTile))
                    // Rule A: station tiles block all trains regardless of direction.
                    occupiedStations.Add(tick.FinalTile);
                else
                    // Rule B: non-station tiles only block trains going the same way.
                    occupiedNonStationByDir.Add((tick.FinalTile, trains[t].Direction));
            }

            // ── Rule C: two moving trains targeting the same station ─────────────
            // Trains are processed in list order; the first to claim a station wins.
            // The loser is blocked at its current tile.
            var stationClaims = new Dictionary<Location, int>(); // tile → winning train index
            for (int t = 0; t < trains.Count; t++)
            {
                var tick = ticks[t];
                bool isMoving = tick.FinalTile != trains[t].TilePosition;
                if (!isMoving || !stationTiles.Contains(tick.FinalTile)) continue;

                if (!stationClaims.TryAdd(tick.FinalTile, t))
                {
                    // A lower-index train already claimed this station — block this one.
                    ticks[t] = tick with
                    {
                        FinalTile      = trains[t].TilePosition,
                        FinalDirection = trains[t].Direction
                    };
                    anyChange = true;
                }
            }

            // ── Rules A and B: block trains targeting occupied tiles ─────────────
            for (int t = 0; t < trains.Count; t++)
            {
                var tick = ticks[t];
                bool isMoving = tick.FinalTile != trains[t].TilePosition;
                if (!isMoving) continue; // already staying — nothing to do

                // Rule A: target station is held by a working or previously blocked train.
                if (occupiedStations.Contains(tick.FinalTile))
                {
                    ticks[t] = tick with
                    {
                        FinalTile      = trains[t].TilePosition,
                        FinalDirection = trains[t].Direction
                    };
                    anyChange = true;
                    continue;
                }

                // Rule B: target non-station tile is held by a same-direction train.
                if (occupiedNonStationByDir.Contains((tick.FinalTile, tick.FinalDirection)))
                {
                    ticks[t] = tick with
                    {
                        FinalTile      = trains[t].TilePosition,
                        FinalDirection = trains[t].Direction
                    };
                    anyChange = true;
                }
            }
        } while (anyChange);

        // ══════════════════════════════════════════════════════════════════════════
        // PHASE 3: Apply resolved tick results.
        // Working trains perform their passenger operation and stay in place.
        // All other trains move to their resolved FinalTile (or stay if blocked).
        // ══════════════════════════════════════════════════════════════════════════
        for (int t = 0; t < trains.Count; t++)
        {
            var tick  = ticks[t];
            var train = trains[t];

            if (tick.HasWork)
            {
                if (tick.DropOff is not null)
                {
                    // Passenger delivered — remove from train and award a point.
                    trains[t] = train with
                    {
                        Passengers = train.Passengers.Where(p => p.Id != tick.DropOff.Id).ToList()
                    };
                    pointsScored++;
                }
                else if (tick.PickUp is not null)
                {
                    // Passenger boarded — remove from the platform and add to the train.
                    waitingPassengers.Remove(tick.PickUp);
                    trains[t] = train with
                    {
                        Passengers = [.. train.Passengers, tick.PickUp with { StationId = null }]
                    };
                }
                // TilePosition and Direction are unchanged — train stays at station.
            }
            else
            {
                // Move to resolved position (same tile as start when blocked).
                trains[t] = train with
                {
                    TilePosition = tick.FinalTile,
                    Direction    = tick.FinalDirection,
                };
            }
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
            // Resource not yet in use — create a new line and mark the resource as used.
            if (resource.InUse)
                return snapshot;

            // Both station IDs must differ — a line cannot start and end at the same station.
            if (action.FromStationId == action.ToStationId)
                return snapshot;

            // Reject if these two stations are already directly connected (adjacent) on any line.
            // "Directly connected" means they appear next to each other in some line's station list.
            if (AreDirectlyConnected(snapshot.Lines, action.FromStationId, action.ToStationId))
                return snapshot;

            var newLine = new Line { LineId = action.LineId, StationIds = [action.FromStationId, action.ToStationId] };
            var updatedResource = resource with { InUse = true };
            return snapshot with
            {
                Lines = [.. snapshot.Lines, newLine],
                Resources = [.. snapshot.Resources.Where(r => r.Id != resource.Id), updatedResource],
            };
        }

        // Line already exists — try to extend it if one end matches FromStationId.
        var stationIds = existingLine.StationIds.ToList();

        if (stationIds[^1] == action.FromStationId)
            stationIds.Add(action.ToStationId);
        else if (stationIds[0] == action.FromStationId)
            stationIds.Insert(0, action.ToStationId);
        else
            return snapshot; // FromStationId is not at either end — ignore.

        // The new terminal station must not already appear anywhere in this line.
        // This prevents duplicates and loops (e.g. extending back to a mid-line station).
        if (existingLine.StationIds.Contains(action.ToStationId))
            return snapshot;

        // The new segment must not duplicate a direct connection that already exists on any line.
        if (AreDirectlyConnected(snapshot.Lines, action.FromStationId, action.ToStationId))
            return snapshot;

        var extendedLine = existingLine with { StationIds = stationIds };
        return snapshot with
        {
            Lines = [.. snapshot.Lines.Where(l => l.LineId != action.LineId), extendedLine],
        };
    }

    /// <summary>
    /// Returns true when <paramref name="stationA"/> and <paramref name="stationB"/> are
    /// already directly connected — i.e. they appear as adjacent entries in any existing line.
    /// </summary>
    private static bool AreDirectlyConnected(IReadOnlyList<Line> lines, Guid stationA, Guid stationB)
    {
        foreach (var line in lines)
        {
            var ids = line.StationIds;
            for (int i = 0; i < ids.Count - 1; i++)
            {
                // Check both orderings because lines are traversed in both directions.
                if ((ids[i] == stationA && ids[i + 1] == stationB) ||
                    (ids[i] == stationB && ids[i + 1] == stationA))
                    return true;
            }
        }
        return false;
    }

    private static GameSnapshot ApplyAddVehicleToLine(GameSnapshot snapshot, AddVehicleToLine action)
    {
        // Must be an available (not in-use) Train resource.
        var resource = snapshot.Resources.FirstOrDefault(
            r => r.Id == action.VehicleId && r.Type == ResourceType.Train && !r.InUse);
        if (resource is null)
            return snapshot;

        // Line must exist and be in use.
        var line = snapshot.Lines.FirstOrDefault(l => l.LineId == action.LineId);
        if (line is null)
            return snapshot;

        // The spawn station must be on the line.
        if (!line.StationIds.Contains(action.StationId))
            return snapshot;

        // Resolve the station to a tile location.
        var stationEntry = snapshot.Stations.FirstOrDefault(kvp => kvp.Value.Id == action.StationId);
        if (stationEntry.Value is null)
            return snapshot;

        // A line cannot have more trains than it has stations (e.g. 3 stations → max 3 trains).
        // This keeps minimum spacing between trains and guarantees the shuttle pattern makes sense.
        var trainsOnLine = snapshot.Trains.Count(t => t.LineId == action.LineId);
        if (trainsOnLine >= line.StationIds.Count)
            return snapshot;

        // Cannot deploy a train at a tile already occupied by another train.
        // This enforces the single-occupancy invariant at the moment of deployment.
        if (snapshot.Trains.Any(t => t.TilePosition == stationEntry.Key))
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