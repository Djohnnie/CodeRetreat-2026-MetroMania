using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;
using MetroMania.Engine.Contracts;
using MetroMania.Engine.Model;
using System.Diagnostics;

namespace MetroMania.Engine;

/// <summary>
/// Deterministic, tick-based simulation engine for MetroMania.
///
/// Each call to <see cref="Run"/> or <see cref="RunSimulation"/> is completely
/// self-contained: the same <paramref name="level"/> configuration and bot
/// <see cref="IMetroManiaRunner"/> implementation will always produce an identical
/// result for a given level seed, regardless of when or where the simulation runs.
///
/// The engine does not own any mutable state between invocations; all state is
/// threaded through immutable <see cref="GameSnapshot"/> records.
/// </summary>
public class MetroManiaEngine
{
    /// <summary>
    /// Executes the full simulation and returns a <see cref="GameResult"/> containing
    /// the final score, wall-clock processing time, and the complete snapshot history
    /// (one <see cref="GameSnapshot"/> per hour) suitable for replay and rendering.
    /// </summary>
    /// <param name="runner">
    ///   The bot that responds to simulation events and supplies <see cref="PlayerAction"/>
    ///   decisions at the end of every hour.
    /// </param>
    /// <param name="level">
    ///   Level definition: grid dimensions, station spawn schedule, passenger phases,
    ///   weekly gift overrides, vehicle capacity, and the RNG seed.
    /// </param>
    /// <param name="maxHours">
    ///   Optional hard cap on simulated hours.  Useful for unit tests and preview
    ///   renders that should not run to a natural game-over.
    /// </param>
    /// <param name="cancellationToken">Allows the caller to abort a long-running simulation.</param>
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

    /// <summary>
    /// Core simulation loop. Advances the clock one hour at a time, driving station
    /// spawns, passenger spawns, train movement, weekly gifts, and player actions.
    ///
    /// <para>
    /// <strong>Event ordering within each hour tick:</strong>
    /// <list type="number">
    ///   <item><see cref="IMetroManiaRunner.OnDayStart"/> — called once at hour 0 of each day.</item>
    ///   <item>Station crowding / game-over check — evaluated before new content appears so
    ///         the runner sees the state that caused the crowd, not the subsequent state.</item>
    ///   <item><see cref="SpawnStations"/> — new stations materialise per the level schedule.</item>
    ///   <item><see cref="SpawnPassengers"/> — deterministic RNG spawns per station phase.</item>
    ///   <item><see cref="ProcessTrains"/> — three-phase train movement pipeline and scoring.</item>
    ///   <item>Weekly gift (Monday 00:00 only) — resource added; runner notified.</item>
    ///   <item><see cref="IMetroManiaRunner.OnHourTicked"/> — player returns a <see cref="PlayerAction"/>.</item>
    ///   <item><see cref="ApplyPlayerAction"/> — action is validated and applied to the snapshot.</item>
    ///   <item>Snapshot is appended to the history list.</item>
    /// </list>
    /// </para>
    /// </summary>
    public SimulationResult RunSimulation(IMetroManiaRunner runner, Level level, int? maxHours = null, CancellationToken cancellationToken = default)
    {
        // absoluteHour is the monotonically increasing tick counter; it is the ground truth
        // from which all calendar values (day, hour-of-day, day-of-week) are derived.
        var absoluteHour = 0;
        var snapshots = new List<GameSnapshot>();
        var totalPassengersSpawned = 0;

        // Seed the very first snapshot. Resources are pre-populated from LevelData.InitialResources
        // so tests and demo levels can grant starting resources without waiting for weekly gifts.
        var snapshot = new GameSnapshot
        {
            Time = new GameTime(1, 0, DayOfWeek.Sunday),
            TotalHoursElapsed = 0,
            Score = 0,
            Resources = level.LevelData.InitialResources
                .Select(type => new Resource { Id = Guid.NewGuid(), Type = type, InUse = false })
                .ToList(),
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

            // ── Derive calendar values from the absolute tick counter ──────────
            // Day 1 = hours 0–23, Day 2 = hours 24–47, etc.
            // DayOfWeek wraps every 7 days starting on Sunday (the first day of Day 1).
            var day = absoluteHour / 24 + 1;
            var hourOfDay = absoluteHour % 24;
            var dayOfWeek = (DayOfWeek)(absoluteHour / 24 % 7);
            var gameTime = new GameTime(day, hourOfDay, dayOfWeek);

            // ── Produce a shallow copy of the previous snapshot for this tick ──
            // GameSnapshot is a record; `with` performs a non-destructive copy.
            // We explicitly re-wrap every collection property so that mutations
            // performed this tick (adding a passenger, moving a train, etc.) do not
            // retroactively alter snapshots already stored in the history list.
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

            // ── Station crowding / game-over check ────────────────────────────
            // Evaluated after day-start so the runner sees the state before new
            // stations or passengers appear.  Thresholds: 10+ crowded, 20+ game over.
            const int CrowdedThreshold  = 10;
            const int GameOverThreshold = 20;

            bool gameOver = false;
            foreach (var (_, station) in snapshot.Stations)
            {
                var count = snapshot.Passengers.Count(p => p.StationId == station.Id);
                if (count >= GameOverThreshold)
                {
                    runner.OnGameOver(snapshot, station.Id);
                    snapshots.Add(snapshot);
                    gameOver = true;
                    break;
                }
                if (count >= CrowdedThreshold)
                    runner.OnStationCrowded(snapshot, station.Id, count);
            }
            if (gameOver) break;

            // Spawn stations at the start of the hour before player action
            foreach (var station in SpawnStations(level, snapshot))
            {
                runner.OnStationSpawned(snapshot, station.Id, station.Location, station.StationType);
            }

            var spawnedPassengers = SpawnPassengers(level, snapshot).ToList();
            if (spawnedPassengers.Count > 0)
                snapshot = snapshot with { Passengers = [.. snapshot.Passengers, .. spawnedPassengers.Select(p => p.Passenger)] };
            totalPassengersSpawned += spawnedPassengers.Count;
            foreach (var (stationId, passenger) in spawnedPassengers)
                runner.OnPassengerSpawned(snapshot, stationId, passenger.Id);

            snapshot = ProcessTrains(level, snapshot);

            // ── Weekly gift — granted on the first tick of every Monday ──────────
            // The gift is added before the player's OnHourTicked call so the runner
            // can immediately use the new resource in the same turn it was received.
            if (dayOfWeek == DayOfWeek.Monday && hourOfDay == 0)
            {
                var weeklyGift = GetWeeklyGift(level, snapshot);
                snapshot = snapshot with
                {
                    Resources = [.. snapshot.Resources, new Resource { Type = weeklyGift, InUse = false }]
                };
                runner.OnWeeklyGiftReceived(snapshot, weeklyGift);
            }

            // ── Player action ─────────────────────────────────────────────────
            // OnHourTicked is always the last callback of the tick so the runner
            // sees the fully updated state (post-train-movement, post-gift) before
            // deciding what to do.
            var playerAction = runner.OnHourTicked(snapshot);

            snapshot = ApplyPlayerAction(runner, snapshot with { LastAction = playerAction });

            snapshots.Add(snapshot);
            absoluteHour++;
        }

        return new SimulationResult
        {
            TotalScore = snapshots.Count > 0 ? snapshots[^1].Score : 0,
            DaysSurvived = absoluteHour / 24,
            TotalPassengersSpawned = totalPassengersSpawned,
            NumberOfPlayerActions = snapshots.Count(x => x.LastAction is not NoAction),
            GameSnapshots = snapshots
        };
    }

    /// <summary>
    /// Determines which stations should appear during the current tick and registers
    /// them in <paramref name="snapshot"/>.Stations (mutated in place for efficiency),
    /// then returns the newly spawned station objects so the caller can fire
    /// <see cref="IMetroManiaRunner.OnStationSpawned"/> callbacks.
    ///
    /// A station spawns exactly once: at hour 0 of day <c>SpawnDelayDays + 1</c>.
    /// Each station receives a fresh <see cref="Guid"/> on spawn so identity is
    /// stable for the rest of the simulation.
    /// </summary>
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

    /// <summary>
    /// Yields zero or one passenger per station for the current tick, based on each
    /// station's active spawn phase and how many hours that station has been alive.
    ///
    /// <para>
    /// <strong>Spawn phase selection:</strong> each station can have multiple spawn
    /// phases defined, each activated after a given number of days alive.  The most
    /// recently unlocked phase (highest <c>AfterDays</c> ≤ days-alive) is used.
    /// A phase with <c>FrequencyInHours ≤ 0</c> disables spawning entirely.
    /// </para>
    ///
    /// <para>
    /// <strong>Destination selection:</strong> the destination type is chosen uniformly
    /// at random from all station types currently on the map <em>except</em> the
    /// spawning station's own type, ensuring passengers always need to travel.
    /// The RNG seed formula — <c>level.Seed + absoluteHour × 100 + gridX × 10 + gridY</c>
    /// — guarantees that no two stations share an RNG stream within the same hour,
    /// preserving full determinism.
    /// </para>
    /// </summary>
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

            // Seed each station's RNG independently so that adding or removing a station
            // elsewhere on the grid never changes what destination type another station picks.
            // Multiplying absoluteHour by 100 and gridX by 10 ensures the three components
            // never accidentally collide into the same integer for distinct combinations.
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
        int FinalDirection,
        /// <summary>
        /// Index of <see cref="FinalTile"/> within the line's tile path.
        /// Carried forward so the engine never has to call IndexOf (which would return the wrong
        /// occurrence when tiles are duplicated around a turning-point station).
        /// </summary>
        int FinalPathIndex
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
                ticks[t] = new TrainTick(false, null, null, train.TilePosition, train.Direction, train.PathIndex);
                continue;
            }

            // ── Resolve the train's position to a tile-path index ─────────────
            // Use the stored PathIndex when valid to avoid List.IndexOf, which would
            // return the first occurrence of the tile and pick the wrong slot when
            // tiles are duplicated (e.g. tiles shared by the inbound and outbound
            // segments around a turning-point station).
            int idx = (train.PathIndex >= 0
                       && train.PathIndex < tilePath.Count
                       && tilePath[train.PathIndex] == train.TilePosition)
                      ? train.PathIndex
                      : tilePath.IndexOf(train.TilePosition);

            if (idx == -1)
            {
                // Position is not on the current path (e.g. path shrank) — snap to start.
                ticks[t] = new TrainTick(false, null, null, tilePath[0], 1, 0);
                continue;
            }

            // ── Train is at a station: check for passenger work ────────────────
            if (snapshot.Stations.TryGetValue(train.TilePosition, out var currentStation))
            {
                // 1. Drop off a passenger whose destination type matches this station.
                var toDrop = train.Passengers.FirstOrDefault(p => p.DestinationType == currentStation.StationType);
                if (toDrop is not null)
                {
                    ticks[t] = new TrainTick(true, toDrop, null, train.TilePosition, train.Direction, idx);
                    continue;
                }

                // 2. Pick up the oldest waiting passenger (FIFO) whose destination is
                //    reachable in the outgoing travel direction.
                //    At a terminal, the next move flips direction, so we look ahead
                //    using the post-flip direction instead of the stored one.
                bool wouldStepOffPath = idx + train.Direction < 0
                                     || idx + train.Direction >= tilePath.Count;
                int effectiveDir = wouldStepOffPath ? -train.Direction : train.Direction;
                var futureTypes = GetFutureStationTypes(tilePath, idx, effectiveDir, snapshot.Stations);

                var toPickUp = waitingPassengers
                    .Where(p =>
                        p.StationId == currentStation.Id &&
                        train.Passengers.Count < level.LevelData.VehicleCapacity &&
                        futureTypes.Contains(p.DestinationType))
                    .MinBy(p => p.SpawnedAtHour);

                if (toPickUp is not null)
                {
                    ticks[t] = new TrainTick(true, null, toPickUp, train.TilePosition, train.Direction, idx);
                    continue;
                }
            }

            // ── No work at this tick — compute movement ────────────────────────
            if (tilePath.Count < 2)
            {
                // Single-tile path; train cannot move.
                ticks[t] = new TrainTick(false, null, null, train.TilePosition, train.Direction, idx);
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

            ticks[t] = new TrainTick(false, null, null, tilePath[nextIdx], dir, nextIdx);
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
                        FinalDirection = trains[t].Direction,
                        FinalPathIndex = trains[t].PathIndex
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
                        FinalDirection = trains[t].Direction,
                        FinalPathIndex = trains[t].PathIndex
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
                        FinalDirection = trains[t].Direction,
                        FinalPathIndex = trains[t].PathIndex
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
                        Passengers = train.Passengers.Where(p => p.Id != tick.DropOff.Id).ToList(),
                        PathIndex  = tick.FinalPathIndex,
                    };
                    pointsScored++;
                }
                else if (tick.PickUp is not null)
                {
                    // Passenger boarded — remove from the platform and add to the train.
                    waitingPassengers.Remove(tick.PickUp);
                    trains[t] = train with
                    {
                        Passengers = [.. train.Passengers, tick.PickUp with { StationId = null }],
                        PathIndex  = tick.FinalPathIndex,
                    };
                }
                else
                {
                    // No passenger work this tick, but PathIndex is still updated.
                    trains[t] = train with { PathIndex = tick.FinalPathIndex };
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
                    PathIndex    = tick.FinalPathIndex,
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

    /// <summary>
    /// Returns the resource type that should be awarded as the weekly gift on the
    /// given week number (1-based, where week 1 is days 1–7).
    ///
    /// If the level designer has placed a <see cref="WeeklyGiftOverride"/> for the
    /// current week, that override takes effect unconditionally.  Otherwise a seeded
    /// coin-flip (seed = <c>level.Seed + weekNumber</c>) chooses between
    /// <see cref="ResourceType.Line"/> and <see cref="ResourceType.Train"/>,
    /// making the gift sequence reproducible but varied across weeks.
    /// </summary>
    private static ResourceType GetWeeklyGift(Level level, GameSnapshot snapshot)
    {
        var weekNumber = snapshot.TotalHoursElapsed / (24 * 7) + 1;

        var overrride = level.LevelData.WeeklyGiftOverrides.FirstOrDefault(x => x.Week == weekNumber);
        if (overrride is not null)
            return overrride.ResourceType;

        var rng = new Random(level.LevelData.Seed + weekNumber);
        return rng.Next(2) == 0 ? ResourceType.Line : ResourceType.Train;
    }

    /// <summary>
    /// Dispatches the action chosen by the player this tick to the appropriate handler,
    /// calling <see cref="IMetroManiaRunner.OnInvalidPlayerAction"/> if the action was
    /// rejected.  <see cref="NoAction"/> is silently ignored.
    /// </summary>
    private static GameSnapshot ApplyPlayerAction(IMetroManiaRunner runner, GameSnapshot snapshot)
    {
        if (snapshot.LastAction is NoAction)
            return snapshot;

        var (newSnapshot, errorCode, errorDescription) = snapshot.LastAction switch
        {
            CreateLine createLine       => ApplyCreateLine(snapshot, createLine),
            AddVehicleToLine addVehicle => ApplyAddVehicleToLine(snapshot, addVehicle),
            _                           => (snapshot, -1, "Action type not recognised or not yet implemented.")
        };

        // Notify the player when their action had no effect.
        if (errorCode != 0)
            runner.OnInvalidPlayerAction(snapshot, errorCode, errorDescription!);

        return newSnapshot;
    }

    /// <summary>
    /// Handles both the <em>create</em> and <em>extend</em> variants of the
    /// <see cref="CreateLine"/> player action.
    ///
    /// Returns a tuple of (newSnapshot, errorCode, errorDescription).
    /// errorCode == 0 means success; any other value is a <see cref="PlayerActionError"/> constant.
    /// </summary>
    private static (GameSnapshot, int, string?) ApplyCreateLine(GameSnapshot snapshot, CreateLine action)
    {
        var resource = snapshot.Resources.FirstOrDefault(r => r.Id == action.LineId && r.Type == ResourceType.Line);
        if (resource is null)
            return (snapshot, PlayerActionError.LineResourceNotFound,
                $"No line resource with id {action.LineId} found in available resources.");

        var existingLine = snapshot.Lines.FirstOrDefault(l => l.LineId == action.LineId);

        if (existingLine is null)
        {
            // Resource not yet in use — create a new line and mark the resource as used.
            if (resource.InUse)
                return (snapshot, PlayerActionError.LineResourceAlreadyInUse,
                    $"Line resource {action.LineId} is already deployed on the map.");

            // Both station IDs must differ — a line cannot start and end at the same station.
            if (action.FromStationId == action.ToStationId)
                return (snapshot, PlayerActionError.LineStationsSameStation,
                    "FromStationId and ToStationId must be different stations.");

            // Reject if these two stations are already directly connected (adjacent) on any line.
            if (AreDirectlyConnected(snapshot.Lines, action.FromStationId, action.ToStationId))
                return (snapshot, PlayerActionError.LineSegmentAlreadyExists,
                    $"Stations {action.FromStationId} and {action.ToStationId} are already directly connected on an existing line.");

            var newLine = new Line { LineId = action.LineId, OrderId = snapshot.NextLineOrderId, StationIds = [action.FromStationId, action.ToStationId] };
            var updatedResource = resource with { InUse = true };
            return (snapshot with
            {
                Lines = [.. snapshot.Lines, newLine],
                Resources = [.. snapshot.Resources.Where(r => r.Id != resource.Id), updatedResource],
                NextLineOrderId = snapshot.NextLineOrderId + 1,
            }, 0, null);
        }

        // Line already exists — try to extend it if one end matches FromStationId.
        var stationIds = existingLine.StationIds.ToList();

        if (stationIds[^1] == action.FromStationId)
            stationIds.Add(action.ToStationId);
        else if (stationIds[0] == action.FromStationId)
            stationIds.Insert(0, action.ToStationId);
        else
            return (snapshot, PlayerActionError.LineExtendFromNotTerminal,
                $"Station {action.FromStationId} is not at either terminal of line {action.LineId}. Only terminal stations can be used to extend a line.");

        // The new terminal station must not already appear anywhere in this line.
        if (existingLine.StationIds.Contains(action.ToStationId))
            return (snapshot, PlayerActionError.LineExtendToAlreadyOnLine,
                $"Station {action.ToStationId} is already on line {action.LineId}. Duplicate stops and loops are not allowed.");

        // The new segment must not duplicate a direct connection that already exists on any line.
        if (AreDirectlyConnected(snapshot.Lines, action.FromStationId, action.ToStationId))
            return (snapshot, PlayerActionError.LineSegmentAlreadyExists,
                $"Stations {action.FromStationId} and {action.ToStationId} are already directly connected on an existing line.");

        var extendedLine = existingLine with { StationIds = stationIds };
        return (snapshot with
        {
            Lines = [.. snapshot.Lines.Where(l => l.LineId != action.LineId), extendedLine],
        }, 0, null);
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

    /// <summary>
    /// Deploys an available (not in-use) <see cref="ResourceType.Train"/> resource onto
    /// the specified line at the given spawn station tile.
    ///
    /// Returns a tuple of (newSnapshot, errorCode, errorDescription).
    /// errorCode == 0 means success; any other value is a <see cref="PlayerActionError"/> constant.
    /// </summary>
    private static (GameSnapshot, int, string?) ApplyAddVehicleToLine(GameSnapshot snapshot, AddVehicleToLine action)
    {
        // Must be an available (not in-use) Train resource.
        var resource = snapshot.Resources.FirstOrDefault(
            r => r.Id == action.VehicleId && r.Type == ResourceType.Train && !r.InUse);
        if (resource is null)
            return (snapshot, PlayerActionError.TrainResourceNotFound,
                $"No unused train resource with id {action.VehicleId} found.");

        // Line must exist and be in use.
        var line = snapshot.Lines.FirstOrDefault(l => l.LineId == action.LineId);
        if (line is null)
            return (snapshot, PlayerActionError.TrainLineNotFound,
                $"Line {action.LineId} does not exist on the map. Create the line before deploying a train.");

        // The spawn station must be on the line.
        if (!line.StationIds.Contains(action.StationId))
            return (snapshot, PlayerActionError.TrainStationNotOnLine,
                $"Station {action.StationId} is not part of line {action.LineId}.");

        // Resolve the station to a tile location.
        var stationEntry = snapshot.Stations.FirstOrDefault(kvp => kvp.Value.Id == action.StationId);
        if (stationEntry.Value is null)
            return (snapshot, PlayerActionError.TrainStationNotSpawned,
                $"Station {action.StationId} has not yet spawned on the map.");

        // A line cannot have more trains than it has stations.
        var trainsOnLine = snapshot.Trains.Count(t => t.LineId == action.LineId);
        if (trainsOnLine >= line.StationIds.Count)
            return (snapshot, PlayerActionError.TrainLineAtCapacity,
                $"Line {action.LineId} already has {trainsOnLine} train(s), which is the maximum for a {line.StationIds.Count}-station line.");

        // Cannot deploy a train at a tile already occupied by another train.
        if (snapshot.Trains.Any(t => t.TilePosition == stationEntry.Key))
            return (snapshot, PlayerActionError.TrainTileOccupied,
                $"Station {action.StationId} is currently occupied by another train. Wait for it to move before deploying here.");

        var stationLocations = snapshot.Stations.ToDictionary(kvp => kvp.Value.Id, kvp => kvp.Key);
        var tilePath = LinePathHelper.ComputeTilePath(line, stationLocations);
        int pathIndex = Math.Max(0, tilePath.IndexOf(stationEntry.Key));

        var newTrain = new Train
        {
            TrainId   = action.VehicleId,
            LineId    = action.LineId,
            TilePosition = stationEntry.Key,
            Direction = 1,
            PathIndex = pathIndex,
        };

        return (snapshot with
        {
            Trains = [.. snapshot.Trains, newTrain],
            Resources = [.. snapshot.Resources.Where(r => r.Id != resource.Id), resource with { InUse = true }],
        }, 0, null);
    }
}