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
    public GameResult Run(IMetroManiaRunner runner, Level level, int? maxHours = null, bool collectSnapshots = true, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var simulationResult = RunSimulation(runner, level, maxHours, collectSnapshots, cancellationToken);
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
    private static SimulationResult RunSimulation(IMetroManiaRunner runner, Level level, int? maxHours = null, bool collectSnapshots = true, CancellationToken cancellationToken = default)
    {
        // absoluteHour is the monotonically increasing tick counter; it is the ground truth
        // from which all calendar values (day, hour-of-day, day-of-week) are derived.
        var absoluteHour = 0;
        var snapshots = collectSnapshots ? new List<GameSnapshot>() : null;
        var totalPassengersSpawned = 0;
        var numberOfPlayerActions = 0;

        // Seed the very first snapshot. Resources are pre-populated from LevelData.InitialResources
        // so tests and demo levels can grant starting resources without waiting for weekly gifts.
        var snapshot = new GameSnapshot
        {
            Time = new GameTime(1, 0, DayOfWeek.Monday),
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
            // DayOfWeek wraps every 7 days starting on Monday (the first day of Day 1).
            var day = absoluteHour / 24 + 1;
            var hourOfDay = absoluteHour % 24;
            var dayOfWeek = (DayOfWeek)((absoluteHour / 24 + 1) % 7);
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
            const int CrowdedThreshold = 10;
            const int GameOverThreshold = 20;

            bool gameOver = false;
            foreach (var (_, station) in snapshot.Stations)
            {
                var count = snapshot.Passengers.Count(p => p.StationId == station.Id);
                if (count >= GameOverThreshold)
                {
                    runner.OnGameOver(snapshot, station.Id);
                    snapshots?.Add(snapshot);
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

            // ── Finalize pending vehicle removals ────────────────────────────
            // After train movement and passenger ops, check for trains flagged
            // PendingRemoval that have dropped off all their passengers.
            // Remove them, release the resource, and notify the runner.
            snapshot = FinalizePendingRemovals(runner, snapshot);

            // ── Weekly gift — granted on the first tick of every Monday ──────────
            // The gift is added before the player's OnHourTicked call so the runner
            // can immediately use the new resource in the same turn it was received.
            // On the very first tick (week 1), initial resources are already seeded in
            // the snapshot; the runner is notified for each one without adding duplicates.
            // Weekly gift overrides always fire independently, including on week 1.
            if (dayOfWeek == DayOfWeek.Monday && hourOfDay == 0)
            {
                if (absoluteHour == 0)
                {
                    foreach (var resourceType in level.LevelData.InitialResources)
                        runner.OnWeeklyGiftReceived(snapshot, resourceType);
                }

                var weeklyGifts = GetWeeklyGifts(level, snapshot);
                foreach (var gift in weeklyGifts)
                {
                    snapshot = snapshot with
                    {
                        Resources = [.. snapshot.Resources, new Resource { Type = gift, InUse = false }]
                    };
                    runner.OnWeeklyGiftReceived(snapshot, gift);
                }
            }

            // ── Player action ─────────────────────────────────────────────────
            // OnHourTicked is always the last callback of the tick so the runner
            // sees the fully updated state (post-train-movement, post-gift) before
            // deciding what to do.
            var playerAction = runner.OnHourTicked(snapshot);

            snapshot = ApplyPlayerAction(runner, snapshot with { LastAction = playerAction });

            // ── Post-action pending-removal finalization ──────────────────────
            // A RemoveVehicle action sets PendingRemoval on the train.  When the
            // train has zero passengers the removal can complete immediately in
            // the same tick rather than waiting for the next ProcessTrains cycle.
            snapshot = FinalizePendingRemovals(runner, snapshot);

            snapshots?.Add(snapshot);
            if (snapshot.LastAction is not NoAction) numberOfPlayerActions++;
            absoluteHour++;
        }

        return new SimulationResult
        {
            TotalScore = snapshot.Score,
            DaysSurvived = absoluteHour / 24,
            TotalPassengersSpawned = totalPassengersSpawned,
            NumberOfPlayerActions = numberOfPlayerActions,
            GameSnapshots = snapshots ?? []
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
    ///   A. Station occupation (direction-agnostic): only one train per line at a station tile at a time.
    ///      Trains from different lines are on independent tracks and coexist freely at stations.
    ///   B. Non-station same-direction (same-line only): a blocked (held) train on open track prevents
    ///      trains on the same line behind it (same direction) from advancing into its tile.
    ///      Trains going in the opposite direction, or trains on other lines, may cross freely.
    ///   C. Simultaneous station arrival (same-line only): when two moving trains on the same line
    ///      target the same station in the same tick the lower-index train wins; the other is blocked.
    ///      Trains on different lines may both arrive at the same station simultaneously.
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
        var trainLines = trains
            .Select(t => snapshot.Lines.FirstOrDefault(l => l.LineId == t.LineId))
            .ToArray();

        var tilePaths = trainLines
            .Select(line => line is null ? (List<Location>)[] : LinePathHelper.ComputeTilePath(line, stationLocations))
            .ToArray();

        // Station IDs belonging to each train's line — used to skip pass-through stations.
        var lineStationSets = trainLines
            .Select(line => line is null ? new HashSet<Guid>() : new HashSet<Guid>(line.StationIds))
            .ToArray();

        // ── Station graph for optimal-route decisions ─────────────────────────────
        // Built once per tick; used by the boarding and transfer-drop checks below.
        var stationById = snapshot.Stations.ToDictionary(kvp => kvp.Value.Id, kvp => kvp.Value);
        var stationAdj = BuildStationAdjacency(stationById, stationLocations, snapshot.Lines);

        // Cache Dijkstra results keyed by (fromStation, destType) to avoid recomputing
        // the same shortest-path query for multiple passengers or trains in one tick.
        var shortestCache = new Dictionary<(Guid, StationType), int>();
        int CachedShortestSteps(Guid from, StationType dest)
        {
            var key = (from, dest);
            if (!shortestCache.TryGetValue(key, out int d))
                shortestCache[key] = d = ShortestStepsToType(stationById, stationAdj, from, dest);
            return d;
        }

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

            // ── Train is at a station on its line: check for passenger work ──
            if (snapshot.Stations.TryGetValue(train.TilePosition, out var currentStation)
                && lineStationSets[t].Contains(currentStation.Id))
            {
                // Direction after any terminal reversal — used for look-ahead checks.
                bool wouldStepOffPath = idx + train.Direction < 0
                                     || idx + train.Direction >= tilePath.Count;
                int effectiveDir = wouldStepOffPath ? -train.Direction : train.Direction;

                // 1. Deliver a carried passenger whose destination type matches this station.
                var toDrop = train.Passengers.FirstOrDefault(p => p.DestinationType == currentStation.StationType);
                if (toDrop is not null)
                {
                    ticks[t] = new TrainTick(true, toDrop, null, train.TilePosition, train.Direction, idx);
                    continue;
                }

                // 1b. Pending-removal forced drop: when the train is scheduled for removal,
                //     force-drop the first remaining passenger at this station even if the
                //     station type doesn't match. The passenger is re-queued (no score) and
                //     can be collected by another train later.
                if (train.PendingRemoval && train.Passengers.Count > 0)
                {
                    var forceDrop = train.Passengers[0];
                    ticks[t] = new TrainTick(true, forceDrop, null, train.TilePosition, train.Direction, idx);
                    continue;
                }

                // Pre-compute the next station the train will visit — shared by both
                // the transfer-drop check and the boarding check below.
                //
                // We resolve the intended next station from the line's station order rather
                // than scanning the tile path for any station tile.  This is necessary because
                // a segment's tile path can physically pass through the grid location of another
                // station on the same line (e.g. Triangle→Circle whose diagonal crosses the
                // Rectangle tile), which would make the boarding check incorrectly conclude that
                // the "next station" is the mid-segment pass-through rather than the real neighbour.
                int effStep = effectiveDir > 0 ? 1 : -1;
                Guid nextStationId = Guid.Empty;
                int nextPathIdx = -1;
                var trainLine = trainLines[t];
                if (trainLine is not null)
                {
                    int stationLineIdx = trainLine.StationIds
                        .Select((id, i) => (id, i))
                        .FirstOrDefault(x => x.id == currentStation.Id, (id: Guid.Empty, i: -1)).i;
                    int nextLineIdx = stationLineIdx + (effectiveDir > 0 ? 1 : -1);
                    if (nextLineIdx >= 0 && nextLineIdx < trainLine.StationIds.Count)
                    {
                        var intendedNextId = trainLine.StationIds[nextLineIdx];
                        // Find this specific station in the tile path, scanning in the travel
                        // direction.  Skips any mid-segment pass-throughs of other stations.
                        for (int i = idx + effStep; i >= 0 && i < tilePath.Count; i += effStep)
                        {
                            if (snapshot.Stations.TryGetValue(tilePath[i], out var ns)
                                && ns.Id == intendedNextId)
                            {
                                nextStationId = intendedNextId;
                                nextPathIdx = i;
                                break;
                            }
                        }
                    }
                }
                int stepsToNext = nextPathIdx >= 0 ? Math.Abs(nextPathIdx - idx) : int.MaxValue;

                // 2. Intermediate transfer drop: a carried passenger can reach their
                //    destination faster by transferring to another line from here.
                //    Skip the drop if the NEXT station on this line is already on the
                //    globally optimal path — in that case let the passenger ride forward
                //    and the transfer will happen at the right interchange station.
                var toTransfer = train.Passengers.FirstOrDefault(p =>
                {
                    int overall = CachedShortestSteps(currentStation.Id, p.DestinationType);
                    if (overall == int.MaxValue) return false; // unreachable everywhere — keep on train
                    int viaLine = MinStepsViaLine(tilePath, idx, effectiveDir, snapshot.Stations, p.DestinationType);
                    if (overall >= viaLine) return false; // staying on this line is already optimal
                    // If the next station is on the globally optimal path, the passenger
                    // is making progress — carry them forward to the interchange.
                    if (nextStationId != Guid.Empty && stepsToNext != int.MaxValue)
                    {
                        int fromNext = CachedShortestSteps(nextStationId, p.DestinationType);
                        if (fromNext != int.MaxValue && stepsToNext + fromNext == overall) return false;
                    }
                    return true;
                });
                if (toTransfer is not null)
                {
                    ticks[t] = new TrainTick(true, toTransfer, null, train.TilePosition, train.Direction, idx);
                    continue;
                }

                // 3. Pick up a waiting passenger — but only if this train is on the
                //    globally optimal route to that passenger's destination.
                //    Trains with PendingRemoval do NOT pick up new passengers.
                if (nextStationId != Guid.Empty && !train.PendingRemoval)
                {
                    var toPickUp = waitingPassengers
                        .Where(p =>
                        {
                            if (p.StationId != currentStation.Id) return false;
                            if (train.Passengers.Count >= level.LevelData.VehicleCapacity) return false;
                            int fromHere = CachedShortestSteps(currentStation.Id, p.DestinationType);
                            if (fromHere == int.MaxValue) return false;
                            int fromNext = CachedShortestSteps(nextStationId, p.DestinationType);
                            // Board only if the next station is on a globally optimal path.
                            return fromNext != int.MaxValue && stepsToNext + fromNext == fromHere;
                        })
                        .MinBy(p => p.SpawnedAtHour);

                    if (toPickUp is not null)
                    {
                        ticks[t] = new TrainTick(true, null, toPickUp, train.TilePosition, train.Direction, idx);
                        continue;
                    }
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
            //
            // Collision rules are scoped per line: trains on different lines share
            // physical tile segments independently (like separate tracks) and must
            // never block each other, otherwise a cross-line deadlock can occur when
            // two lines share the same tile segment.
            var occupiedStationsByLine = new Dictionary<Location, HashSet<Guid>>();
            var occupiedNonStationByDirAndLine = new HashSet<(Location Tile, int Dir, Guid LineId)>();

            for (int t = 0; t < trains.Count; t++)
            {
                var tick = ticks[t];
                bool isStaying = tick.FinalTile == trains[t].TilePosition;
                if (!isStaying) continue;

                var lineId = trains[t].LineId;
                if (stationTiles.Contains(tick.FinalTile))
                {
                    // Rule A: station tiles block same-line trains.
                    if (!occupiedStationsByLine.TryGetValue(tick.FinalTile, out var lineSet))
                        occupiedStationsByLine[tick.FinalTile] = lineSet = [];
                    lineSet.Add(lineId);
                }
                else
                {
                    // Rule B: non-station tiles only block same-line trains going the same way.
                    occupiedNonStationByDirAndLine.Add((tick.FinalTile, trains[t].Direction, lineId));
                }
            }

            // ── Rule C: two moving trains on the same line targeting the same station ──
            // Trains are processed in list order; the first to claim a station wins.
            // The loser is blocked at its current tile.
            // Trains on different lines may both arrive at the same station independently.
            var stationClaims = new Dictionary<(Location Tile, Guid LineId), int>();
            for (int t = 0; t < trains.Count; t++)
            {
                var tick = ticks[t];
                bool isMoving = tick.FinalTile != trains[t].TilePosition;
                if (!isMoving || !stationTiles.Contains(tick.FinalTile)) continue;

                if (!stationClaims.TryAdd((tick.FinalTile, trains[t].LineId), t))
                {
                    // A lower-index train on the same line already claimed this station.
                    ticks[t] = tick with
                    {
                        FinalTile = trains[t].TilePosition,
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

                var lineId = trains[t].LineId;

                // Rule A: target station is held by a same-line working or blocked train.
                if (occupiedStationsByLine.TryGetValue(tick.FinalTile, out var lineSet)
                    && lineSet.Contains(lineId))
                {
                    ticks[t] = tick with
                    {
                        FinalTile = trains[t].TilePosition,
                        FinalDirection = trains[t].Direction,
                        FinalPathIndex = trains[t].PathIndex
                    };
                    anyChange = true;
                    continue;
                }

                // Rule B: target non-station tile is held by a same-line same-direction train.
                if (occupiedNonStationByDirAndLine.Contains((tick.FinalTile, tick.FinalDirection, lineId)))
                {
                    ticks[t] = tick with
                    {
                        FinalTile = trains[t].TilePosition,
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
            var tick = ticks[t];
            var train = trains[t];

            if (tick.HasWork)
            {
                if (tick.DropOff is not null)
                {
                    var dropped = tick.DropOff;
                    trains[t] = train with
                    {
                        Passengers = train.Passengers.Where(p => p.Id != dropped.Id).ToList(),
                        PathIndex = tick.FinalPathIndex,
                    };

                    // Award a point only for final delivery (destination type matches this station).
                    // For intermediate transfer drops the passenger is re-queued at this station
                    // so another train can collect them on the optimal route.
                    snapshot.Stations.TryGetValue(train.TilePosition, out var dropStation);
                    if (dropStation?.StationType == dropped.DestinationType)
                    {
                        pointsScored++;
                    }
                    else if (dropStation is not null)
                    {
                        waitingPassengers.Add(dropped with { StationId = dropStation.Id });
                    }
                }
                else if (tick.PickUp is not null)
                {
                    // Passenger boarded — remove from the platform and add to the train.
                    // Guard against duplicate pickup: two trains from different lines may
                    // simultaneously be at the same station and both select the same passenger
                    // in Phase 1. Only the first train to process in Phase 3 boards them.
                    if (waitingPassengers.Remove(tick.PickUp))
                    {
                        trains[t] = train with
                        {
                            Passengers = [.. train.Passengers, tick.PickUp with { StationId = null }],
                            PathIndex = tick.FinalPathIndex,
                        };
                    }
                    else
                    {
                        // Passenger already taken by an earlier train this tick — treat as idle.
                        trains[t] = train with { PathIndex = tick.FinalPathIndex };
                    }
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
                    Direction = tick.FinalDirection,
                    PathIndex = tick.FinalPathIndex,
                };
            }
        }

        return snapshot with
        {
            Trains = trains,
            Passengers = waitingPassengers,
            Score = snapshot.Score + pointsScored,
        };
    }

    /// <summary>
    /// Builds an undirected adjacency list over the spawned station graph.
    /// Each edge connects two stations that are adjacent (consecutive) on any line.
    /// Edge weight = Chebyshev tile distance between the two station locations,
    /// which equals the exact number of tile steps a train travels on that segment.
    /// </summary>
    private static Dictionary<Guid, List<(Guid Neighbor, int Cost)>> BuildStationAdjacency(
        Dictionary<Guid, Station> stationById,
        Dictionary<Guid, Location> stationLocations,
        IReadOnlyList<Line> lines)
    {
        var adj = stationById.Keys.ToDictionary(id => id, _ => new List<(Guid, int)>());

        foreach (var line in lines)
        {
            var ids = line.StationIds;
            for (int i = 0; i < ids.Count - 1; i++)
            {
                if (!adj.ContainsKey(ids[i]) || !adj.ContainsKey(ids[i + 1])) continue;
                if (!stationLocations.TryGetValue(ids[i], out var locA)) continue;
                if (!stationLocations.TryGetValue(ids[i + 1], out var locB)) continue;

                int cost = Math.Max(Math.Abs(locA.X - locB.X), Math.Abs(locA.Y - locB.Y));
                adj[ids[i]].Add((ids[i + 1], cost));
                adj[ids[i + 1]].Add((ids[i], cost));
            }
        }
        return adj;
    }

    /// <summary>
    /// Dijkstra shortest tile-step distance from <paramref name="fromStationId"/> to
    /// the nearest spawned station whose type equals <paramref name="destType"/>.
    /// Transfers between lines are free (wait time is ignored).
    /// Returns <see cref="int.MaxValue"/> when the destination type is unreachable.
    /// </summary>
    private static int ShortestStepsToType(
        Dictionary<Guid, Station> stationById,
        Dictionary<Guid, List<(Guid Neighbor, int Cost)>> adj,
        Guid fromStationId,
        StationType destType)
    {
        if (stationById.TryGetValue(fromStationId, out var fs) && fs.StationType == destType)
            return 0;

        var dist = new Dictionary<Guid, int> { [fromStationId] = 0 };
        var pq = new PriorityQueue<Guid, int>();
        pq.Enqueue(fromStationId, 0);

        while (pq.TryDequeue(out var cur, out int d))
        {
            if (d > dist.GetValueOrDefault(cur, int.MaxValue)) continue;
            if (stationById.TryGetValue(cur, out var s) && s.StationType == destType) return d;
            if (!adj.TryGetValue(cur, out var neighbors)) continue;

            foreach (var (nb, cost) in neighbors)
            {
                int nd = d + cost;
                if (nd < dist.GetValueOrDefault(nb, int.MaxValue))
                {
                    dist[nb] = nd;
                    pq.Enqueue(nb, nd);
                }
            }
        }
        return int.MaxValue;
    }

    /// <summary>
    /// Returns the minimum tile steps for a passenger riding on <paramref name="tilePath"/>
    /// (currently at <paramref name="pathIndex"/>, heading in <paramref name="direction"/>)
    /// to reach any station of <paramref name="destType"/> by staying on this train.
    /// The train bounces at terminals; the first encounter in each direction is used.
    /// Returns <see cref="int.MaxValue"/> when the destination type is not on this line.
    /// </summary>
    private static int MinStepsViaLine(
        List<Location> tilePath,
        int pathIndex,
        int direction,
        Dictionary<Location, Station> stations,
        StationType destType)
    {
        // Collect all station tiles on the line in path order.
        var lineStations = new List<(int PathIdx, StationType Type)>();
        for (int i = 0; i < tilePath.Count; i++)
            if (stations.TryGetValue(tilePath[i], out var s))
                lineStations.Add((i, s.StationType));

        if (lineStations.Count == 0) return int.MaxValue;

        int curPos = lineStations.FindIndex(ls => ls.PathIdx == pathIndex);
        if (curPos == -1) return int.MaxValue;

        int best = int.MaxValue;
        int step = direction > 0 ? 1 : -1;

        // ── Forward: first T-type station in the current direction of travel ────
        for (int k = curPos + step; k >= 0 && k < lineStations.Count; k += step)
        {
            if (lineStations[k].Type == destType)
            {
                best = Math.Abs(lineStations[k].PathIdx - lineStations[curPos].PathIdx);
                break;
            }
        }

        // ── Backward: reach the forward terminal first, then traverse back ──────
        // This gives access to stations on the opposite side of the current position.
        int terminalFwd = direction > 0 ? lineStations.Count - 1 : 0;
        int stepsToTerm = Math.Abs(lineStations[terminalFwd].PathIdx - lineStations[curPos].PathIdx);

        for (int k = terminalFwd - step; k >= 0 && k < lineStations.Count; k -= step)
        {
            // Stations in the forward half were already handled above with a lower cost.
            if (step > 0 ? k >= curPos : k <= curPos) continue;

            if (lineStations[k].Type == destType)
            {
                int cost = stepsToTerm + Math.Abs(lineStations[k].PathIdx - lineStations[terminalFwd].PathIdx);
                if (cost < best) best = cost;
                break; // First hit going back from terminal is the closest on this side.
            }
        }

        return best;
    }

    /// <summary>
    /// Returns all resource types that should be awarded as weekly gifts on the
    /// given week number (1-based, where week 1 is days 1–7).
    ///
    /// If the level designer has placed one or more <see cref="WeeklyGiftOverride"/>
    /// entries for the current week, all of them take effect.  Otherwise no gift
    /// is awarded — the level designer has full control over the gifting schedule.
    /// </summary>
    private static List<ResourceType> GetWeeklyGifts(Level level, GameSnapshot snapshot)
    {
        var weekNumber = snapshot.TotalHoursElapsed / (24 * 7) + 1;

        return level.LevelData.WeeklyGiftOverrides
            .Where(x => x.Week == weekNumber)
            .Select(x => x.ResourceType)
            .ToList();
    }

    /// <summary>
    /// Checks for trains with <see cref="Train.PendingRemoval"/> that have dropped off
    /// all their passengers. Those trains are removed from the map, their resource is
    /// released (marked as not in use), and <see cref="IMetroManiaRunner.OnVehicleRemoved"/>
    /// is called for each one.
    ///
    /// After train finalization, checks for lines with <see cref="Line.PendingRemoval"/>
    /// that no longer have any trains assigned. Those lines are removed from the map,
    /// their resource is released, and <see cref="IMetroManiaRunner.OnLineRemoved"/> fires.
    /// </summary>
    private static GameSnapshot FinalizePendingRemovals(IMetroManiaRunner runner, GameSnapshot snapshot)
    {
        // ── Train removals ────────────────────────────────────────────────────
        var completedTrainRemovals = snapshot.Trains
            .Where(t => t.PendingRemoval && t.Passengers.Count == 0)
            .ToList();

        if (completedTrainRemovals.Count > 0)
        {
            var removedTrainIds = completedTrainRemovals.Select(t => t.TrainId).ToHashSet();

            snapshot = snapshot with
            {
                Trains = snapshot.Trains.Where(t => !removedTrainIds.Contains(t.TrainId)).ToList(),
                Resources = snapshot.Resources.Select(r =>
                    removedTrainIds.Contains(r.Id) && r.Type == ResourceType.Train
                        ? r with { InUse = false }
                        : r).ToList(),
            };

            foreach (var train in completedTrainRemovals)
                runner.OnVehicleRemoved(snapshot, train.TrainId);
        }

        // ── Line removals ─────────────────────────────────────────────────────
        var completedLineRemovals = snapshot.Lines
            .Where(l => l.PendingRemoval && !snapshot.Trains.Any(t => t.LineId == l.LineId))
            .ToList();

        if (completedLineRemovals.Count > 0)
        {
            var removedLineIds = completedLineRemovals.Select(l => l.LineId).ToHashSet();

            snapshot = snapshot with
            {
                Lines = snapshot.Lines.Where(l => !removedLineIds.Contains(l.LineId)).ToList(),
                Resources = snapshot.Resources.Select(r =>
                    removedLineIds.Contains(r.Id) && r.Type == ResourceType.Line
                        ? r with { InUse = false }
                        : r).ToList(),
            };

            foreach (var line in completedLineRemovals)
                runner.OnLineRemoved(snapshot, line.LineId);
        }

        return snapshot;
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
            CreateLine createLine => ApplyCreateLine(snapshot, createLine),
            ExtendLineFromTerminal extendLine => ApplyExtendLineFromTerminal(snapshot, extendLine),
            ExtendLineInBetween insertStation => ApplyExtendLineInBetween(snapshot, insertStation),
            AddVehicleToLine addVehicle => ApplyAddVehicleToLine(snapshot, addVehicle),
            RemoveVehicle removeVehicle => ApplyRemoveVehicle(snapshot, removeVehicle),
            RemoveLine removeLine => ApplyRemoveLine(snapshot, removeLine),
            _ => (snapshot, -1, "Action type not recognised or not yet implemented.")
        };

        // Notify the player when their action had no effect.
        if (errorCode != 0)
        {
            runner.OnInvalidPlayerAction(snapshot, errorCode, errorDescription!);
            // The action didn't happen — don't record it in the snapshot so the
            // renderer doesn't draw a label for something that had no effect.
            return newSnapshot with { LastAction = new NoAction() };
        }

        return newSnapshot;
    }

    /// <summary>
    /// Creates a brand-new line from two stations, consuming an unused Line resource.
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

        if (resource.InUse)
            return (snapshot, PlayerActionError.LineResourceAlreadyInUse,
                $"Line resource {action.LineId} is already deployed on the map.");

        // Both station IDs must differ — a line cannot start and end at the same station.
        if (action.FromStationId == action.ToStationId)
            return (snapshot, PlayerActionError.LineStationsSameStation,
                "FromStationId and ToStationId must be different stations.");

        var newLine = new Line { LineId = action.LineId, OrderId = snapshot.NextLineOrderId, StationIds = [action.FromStationId, action.ToStationId] };
        var updatedResource = resource with { InUse = true };
        return (snapshot with
        {
            Lines = [.. snapshot.Lines, newLine],
            Resources = [.. snapshot.Resources.Where(r => r.Id != resource.Id), updatedResource],
            NextLineOrderId = snapshot.NextLineOrderId + 1,
        }, 0, null);
    }

    /// <summary>
    /// Extends an existing line from one of its terminal stations to a new station.
    ///
    /// Returns a tuple of (newSnapshot, errorCode, errorDescription).
    /// errorCode == 0 means success; any other value is a <see cref="PlayerActionError"/> constant.
    /// </summary>
    private static (GameSnapshot, int, string?) ApplyExtendLineFromTerminal(GameSnapshot snapshot, ExtendLineFromTerminal action)
    {
        var existingLine = snapshot.Lines.FirstOrDefault(l => l.LineId == action.LineId);
        if (existingLine is null)
            return (snapshot, PlayerActionError.LineExtendLineNotFound,
                $"No line with id {action.LineId} exists on the map.");

        var stationIds = existingLine.StationIds.ToList();

        if (stationIds[^1] == action.TerminalStationId)
            stationIds.Add(action.ToStationId);
        else if (stationIds[0] == action.TerminalStationId)
            stationIds.Insert(0, action.ToStationId);
        else
            return (snapshot, PlayerActionError.LineExtendFromNotTerminal,
                $"Station {action.TerminalStationId} is not at either terminal of line {action.LineId}. Only terminal stations can be used to extend a line.");

        // The new terminal station must not already appear anywhere in this line.
        if (existingLine.StationIds.Contains(action.ToStationId))
            return (snapshot, PlayerActionError.LineExtendToAlreadyOnLine,
                $"Station {action.ToStationId} is already on line {action.LineId}. Duplicate stops and loops are not allowed.");

        var extendedLine = existingLine with { StationIds = stationIds };
        return (snapshot with
        {
            Lines = [.. snapshot.Lines.Where(l => l.LineId != action.LineId), extendedLine],
        }, 0, null);
    }

    /// <summary>
    /// Inserts a station between two consecutive stations on an existing line.
    /// Trains on the affected line have their <see cref="Train.PathIndex"/> recalculated
    /// to match the new tile path.
    ///
    /// Returns a tuple of (newSnapshot, errorCode, errorDescription).
    /// errorCode == 0 means success; any other value is a <see cref="PlayerActionError"/> constant.
    /// </summary>
    private static (GameSnapshot, int, string?) ApplyExtendLineInBetween(GameSnapshot snapshot, ExtendLineInBetween action)
    {
        var existingLine = snapshot.Lines.FirstOrDefault(l => l.LineId == action.LineId);
        if (existingLine is null)
            return (snapshot, PlayerActionError.LineInsertLineNotFound,
                $"No line with id {action.LineId} exists on the map.");

        var stationIds = existingLine.StationIds.ToList();
        int fromIdx = stationIds.IndexOf(action.FromStationId);
        int toIdx = stationIds.IndexOf(action.ToStationId);

        // From and To must both be on the line and consecutive (in either order).
        bool consecutive = fromIdx >= 0 && toIdx >= 0
                        && Math.Abs(fromIdx - toIdx) == 1;
        if (!consecutive)
            return (snapshot, PlayerActionError.LineInsertStationsNotConsecutive,
                $"Stations {action.FromStationId} and {action.ToStationId} are not consecutive on line {action.LineId}.");

        if (stationIds.Contains(action.NewStationId))
            return (snapshot, PlayerActionError.LineInsertStationAlreadyOnLine,
                $"Station {action.NewStationId} is already on line {action.LineId}. Duplicate stops are not allowed.");

        var newStationEntry = snapshot.Stations.FirstOrDefault(kvp => kvp.Value.Id == action.NewStationId);
        if (newStationEntry.Value is null)
            return (snapshot, PlayerActionError.LineInsertStationNotSpawned,
                $"Station {action.NewStationId} has not yet spawned on the map.");

        // Insert the new station between the two consecutive stations.
        int insertAt = Math.Min(fromIdx, toIdx) + 1;
        stationIds.Insert(insertAt, action.NewStationId);

        var updatedLine = existingLine with { StationIds = stationIds };

        // Recompute the tile path and fix up PathIndex for every train on this line.
        var stationLocations = snapshot.Stations.ToDictionary(kvp => kvp.Value.Id, kvp => kvp.Key);
        var newTilePath = LinePathHelper.ComputeTilePath(updatedLine, stationLocations);

        var updatedTrains = snapshot.Trains.Select(train =>
        {
            if (train.LineId != action.LineId) return train;

            int newIdx = newTilePath.IndexOf(train.TilePosition);
            if (newIdx >= 0)
                return train with { PathIndex = newIdx };

            // Tile no longer on path — snap to the closest tile.
            int bestIdx = 0;
            int bestDist = int.MaxValue;
            for (int i = 0; i < newTilePath.Count; i++)
            {
                int dist = Math.Abs(newTilePath[i].X - train.TilePosition.X)
                         + Math.Abs(newTilePath[i].Y - train.TilePosition.Y);
                if (dist < bestDist) { bestDist = dist; bestIdx = i; }
            }
            return train with { PathIndex = bestIdx, TilePosition = newTilePath[bestIdx] };
        }).ToList();

        return (snapshot with
        {
            Lines = [.. snapshot.Lines.Where(l => l.LineId != action.LineId), updatedLine],
            Trains = updatedTrains,
        }, 0, null);
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
            TrainId = action.VehicleId,
            LineId = action.LineId,
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

    /// <summary>
    /// Handles the <see cref="RemoveVehicle"/> player action.
    ///
    /// The train is flagged with <see cref="Train.PendingRemoval"/>. A pending-removal
    /// train continues moving and dropping off passengers but will NOT pick up new ones.
    /// When the train has no passengers left, <see cref="FinalizePendingRemovals"/> removes
    /// it and fires <see cref="IMetroManiaRunner.OnVehicleRemoved"/>. For trains that are
    /// already empty this happens immediately within the same tick.
    ///
    /// Returns a tuple of (newSnapshot, errorCode, errorDescription).
    /// errorCode == 0 means success; any other value is a <see cref="PlayerActionError"/> constant.
    /// </summary>
    private static (GameSnapshot, int, string?) ApplyRemoveVehicle(GameSnapshot snapshot, RemoveVehicle action)
    {
        var train = snapshot.Trains.FirstOrDefault(t => t.TrainId == action.VehicleId);
        if (train is null)
            return (snapshot, PlayerActionError.RemoveVehicleNotFound,
                $"No active train with id {action.VehicleId} found on the map.");

        if (train.PendingRemoval)
            return (snapshot, PlayerActionError.RemoveVehicleAlreadyPending,
                $"Train {action.VehicleId} is already scheduled for removal.");

        // Always flag for pending removal. FinalizePendingRemovals (called after
        // ApplyPlayerAction) will handle the actual removal once the train has
        // zero passengers — which may be immediately if the train is already empty.
        return (snapshot with
        {
            Trains = snapshot.Trains.Select(t =>
                t.TrainId == action.VehicleId ? t with { PendingRemoval = true } : t).ToList(),
        }, 0, null);
    }

    /// <summary>
    /// Handles the <see cref="RemoveLine"/> player action.
    ///
    /// The line is flagged with <see cref="Line.PendingRemoval"/> and all trains
    /// currently on the line are also flagged with <see cref="Train.PendingRemoval"/>
    /// (unless they already are). Those trains will drop off their remaining
    /// passengers and be removed by <see cref="FinalizePendingRemovals"/>.
    /// Once the last train has been removed the line itself is removed and
    /// <see cref="IMetroManiaRunner.OnLineRemoved"/> fires.
    ///
    /// If the line has no trains, it is flagged and removed in the same tick
    /// by the post-action <see cref="FinalizePendingRemovals"/> call.
    /// </summary>
    private static (GameSnapshot, int, string?) ApplyRemoveLine(GameSnapshot snapshot, RemoveLine action)
    {
        var line = snapshot.Lines.FirstOrDefault(l => l.LineId == action.LineId);
        if (line is null)
            return (snapshot, PlayerActionError.RemoveLineNotFound,
                $"No active line with id {action.LineId} found on the map.");

        if (line.PendingRemoval)
            return (snapshot, PlayerActionError.RemoveLineAlreadyPending,
                $"Line {action.LineId} is already scheduled for removal.");

        // Flag the line and all its trains for pending removal.
        return (snapshot with
        {
            Lines = snapshot.Lines.Select(l =>
                l.LineId == action.LineId ? l with { PendingRemoval = true } : l).ToList(),
            Trains = snapshot.Trains.Select(t =>
                t.LineId == action.LineId && !t.PendingRemoval
                    ? t with { PendingRemoval = true }
                    : t).ToList(),
        }, 0, null);
    }
}