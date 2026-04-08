# MetroMania Engine Rules

A comprehensive reference of every feature and rule in the MetroMania simulation engine.
MetroMania is a deterministic, tick-based metro network simulation inspired by Mini Metro.
Players write C# bots that respond to game events and build a metro network to transport passengers between stations.

---

## Table of Contents

- [1. Game Loop & Time](#1-game-loop--time)
- [2. Stations](#2-stations)
- [3. Passengers](#3-passengers)
- [4. Resources](#4-resources)
- [5. Weekly Gifts](#5-weekly-gifts)
- [6. Player Actions](#6-player-actions)
- [7. Lines](#7-lines)
- [8. Trains](#8-trains)
- [9. Train Movement](#9-train-movement)
- [10. Passenger Pickup & Drop-off](#10-passenger-pickup--drop-off)
- [11. Routing & Transfers](#11-routing--transfers)
- [12. Collision Rules](#12-collision-rules)
- [13. Scoring](#13-scoring)
- [14. Station Crowding & Game Over](#14-station-crowding--game-over)
- [15. Event Callbacks](#15-event-callbacks)
- [16. Game Snapshot](#16-game-snapshot)
- [17. Determinism](#17-determinism)
- [18. Visual Reference](#18-visual-reference)

---

## 1. Game Loop & Time

The simulation advances one **hour** per tick. All game time is derived from a single monotonically increasing tick counter.

### Rules

| Rule | Detail |
|------|--------|
| One tick = one hour | Every iteration of the main loop represents one in-game hour. |
| Day = 24 hours | Day 1 spans ticks 0–23, Day 2 spans ticks 24–47, and so on. |
| Day numbering is 1-indexed | The first day is Day 1 (not Day 0). |
| Hour is 0-indexed | Hours range from 0 (midnight) to 23 (11 PM). |
| Day 1 is Monday | The simulation always starts on a Monday. |
| `DayOfWeek` cycles every 7 days | Monday (1) → Tuesday (2) → … → Sunday (0) → Monday (1). |
| `TotalHoursElapsed` is 0-indexed | The first tick has `TotalHoursElapsed = 0`. |
| N hours produces N snapshots | Running for N hours produces exactly N snapshots (ticks 0 through N-1). |
| MaxDays | The simulation ends cleanly after the last hour of `MaxDays` without triggering game over. 0 = no limit. Default: 200. |

### Formulas

```
day       = absoluteHour / 24 + 1
hourOfDay = absoluteHour % 24
dayOfWeek = (DayOfWeek)((absoluteHour / 24 + 1) % 7)
```

### Event Order Per Tick

Every hour, the engine fires events in this exact order:

```
┌─────────────────────────────────────────────────────┐
│                    HOUR TICK                         │
├─────────────────────────────────────────────────────┤
│  1. OnDayStart         (only at hour 0 of each day) │
│  2. Crowding / GameOver check                       │
│  3. Station spawning   → OnStationSpawned           │
│  4. Passenger spawning → OnPassengerSpawned          │
│  5. Train movement     (3-phase pipeline)           │
│  5b. Finalize pending removals                      │
│      → OnVehicleRemoved → OnLineRemoved             │
│  6. Weekly gift        → OnWeeklyGiftReceived        │
│     (only Monday 00:00)                             │
│  7. OnHourTicked       → Player returns action      │
│  8. Apply player action                             │
│  8b. Finalize pending removals (post-action)        │
│      → OnVehicleRemoved → OnLineRemoved             │
│  9. Snapshot recorded                               │
└─────────────────────────────────────────────────────┘
```

> **Key insight:** The player sees the fully updated state (post-train-movement, post-gift) in `OnHourTicked` before deciding their action.

---

## 2. Stations

Stations are fixed locations on a tile grid where passengers appear and wait. Each station has a distinct **shape** that determines which passengers want to travel there.

### Station Types

There are **6 station types**, each with a unique visual shape:

<p align="center">
<img src="resources/station-circle.svg" width="48" height="48" alt="Circle" />
<img src="resources/station-square.svg" width="48" height="48" alt="Rectangle" />
<img src="resources/station-triangle.svg" width="48" height="48" alt="Triangle" />
<img src="resources/station-diamond.svg" width="48" height="48" alt="Diamond" />
<img src="resources/station-pentagon.svg" width="48" height="48" alt="Pentagon" />
<img src="resources/station-star.svg" width="48" height="48" alt="Star" />
</p>

| Type | Shape |
|------|-------|
| `Circle` | ○ Circle |
| `Rectangle` | □ Square |
| `Triangle` | △ Triangle |
| `Diamond` | ◇ Diamond (rotated square) |
| `Pentagon` | ⬠ Pentagon |
| `Star` | ☆ Five-pointed star |

### Rules

| Rule | Detail |
|------|--------|
| Stations are placed on a tile grid | Each station occupies exactly one tile at integer `(X, Y)` coordinates. |
| Each station has a unique shape | The shape determines which passengers want to travel to that station. |
| Stations have a spawn delay | `SpawnDelayDays = N` means the station appears on Day **N+1** at Hour 0. |
| Delay 0 = immediate | A station with `SpawnDelayDays = 0` appears on the very first tick (Day 1, Hour 0). |
| Stations spawn exactly once | Once spawned, a station never disappears or re-spawns. |
| Stations get a unique ID on spawn | A fresh `Guid` is assigned when the station materializes on the map. |
| Stations are keyed by location | Stored in the snapshot as `Dictionary<Location, Station>`. |
| Stations that haven't spawned yet don't exist | They are absent from the snapshot until their spawn day. |

### Spawn Timing Examples

| SpawnDelayDays | Appears On | At Tick |
|:-:|:-:|:-:|
| 0 | Day 1, Hour 0 | 0 |
| 1 | Day 2, Hour 0 | 24 |
| 2 | Day 3, Hour 0 | 48 |
| 5 | Day 6, Hour 0 | 120 |

---

## 3. Passengers

Passengers spawn at stations and want to travel to a station of a specific **type**. They appear as small shape icons indicating their destination.

### Rules

| Rule | Detail |
|------|--------|
| Passengers spawn based on phases | Each station has a list of `PassengerSpawnPhase` entries that control how often passengers appear. |
| Active phase = highest unlocked | The phase with the highest `AfterDays ≤ daysAlive` is active. |
| `daysAlive` is relative to station spawn | `daysAlive = (TotalHoursElapsed - SpawnDelayDays × 24) / 24` |
| Spawn frequency | A passenger spawns when `hoursAlive % FrequencyInHours == 0` (including at `hoursAlive = 0`). |
| FrequencyInHours ≤ 0 disables spawning | No passengers spawn when the active phase has frequency ≤ 0. |
| No phases = no passengers | Stations with an empty `PassengerSpawnPhases` list never spawn passengers. |
| Destination ≠ origin | A passenger's `DestinationType` is always different from the station's own type. |
| Destination is random | Chosen uniformly at random from all station types **currently on the map** (except the origin type). |
| Each passenger gets a unique ID | A fresh `Guid` is assigned on spawn. |
| Max 1 passenger per station per tick | Each station spawns at most one passenger per hour. |
| `SpawnedAtHour` tracks age | Records the `TotalHoursElapsed` when the passenger was created (used for pickup priority: oldest first). |

### Phase Activation Example

A station with `SpawnDelayDays = 0` and the following phases:

| Phase | AfterDays | FrequencyInHours | Effect |
|:-:|:-:|:-:|---|
| 1 | 0 | 24 | Spawns 1 passenger per day from tick 0 |
| 2 | 2 | 12 | After 2 days alive, spawns every 12 hours |
| 3 | 4 | 6 | After 4 days alive, spawns every 6 hours |

> As a station ages, later phases activate and passengers spawn more frequently — increasing difficulty over time.

### RNG Seed Formula

Each station's passenger destination is determined by a per-station, per-tick seed:

```
seed = level.Seed + TotalHoursElapsed × 100 + GridX × 10 + GridY
```

The multipliers (100, 10) prevent collisions between distinct grid coordinates, ensuring each station has an independent RNG stream. Adding or removing a station elsewhere on the grid **never** changes what destination another station picks.

---

## 4. Resources

Resources are consumable items that the player uses to build their metro network. There are two types:

| Type | Purpose |
|------|---------|
| `Line` | Consumed to create a new metro line connecting two stations. |
| `Train` | Consumed to deploy a train on an existing line. |

### Rules

| Rule | Detail |
|------|--------|
| Resources start unused | When granted (via weekly gift or initial resources), `InUse = false`. |
| Using a resource marks it as in-use | Creating a line or deploying a train sets `InUse = true`. |
| Each resource has a unique ID | The `Guid` is used to reference specific resources in player actions. |
| Removing a line/train releases the resource | The resource goes back to `InUse = false` and can be re-used. |
| Initial resources | The level can pre-seed resources via `InitialResources` (e.g., start with 1 Line + 1 Train). |

---

## 5. Weekly Gifts

Every Monday at midnight, the player receives a new resource. This is the primary way to grow the metro network.

### Rules

| Rule | Detail |
|------|--------|
| Timing | Every Monday at Hour 0 (ticks 0, 168, 336, 504, …). |
| Week numbering | Week 1 starts at tick 0. `weekNumber = TotalHoursElapsed / (24 × 7) + 1`. |
| Override gifts | If a `WeeklyGiftOverride` exists for the current week, that resource type is used. |
| No override = no gift | Without an override, no gift is awarded. Level designers have full control over the gifting schedule. |
| Initial resources are week-1 gifts | If `InitialResources` is non-empty, those are the week-1 gifts (already in the snapshot). The engine notifies the runner for each one without creating new resources. |
| Gift arrives before OnHourTicked | The resource is added to the snapshot before the player's callback, so it can be used immediately. |

### Gift Timing

| Week | Day | Tick | Hours to Run |
|:-:|:-:|:-:|:-:|
| 1 | 1 | 0 | 1 |
| 2 | 8 | 168 | 169 |
| 3 | 15 | 336 | 337 |
| 4 | 22 | 504 | 505 |

---

## 6. Player Actions

Each tick, the player's `OnHourTicked` callback must return a `PlayerAction`. Only **one action per tick** is allowed.

### Action Types

| Action | Purpose |
|--------|---------|
| `NoAction` | Do nothing this tick. |
| `CreateLine(LineId, FromStationId, ToStationId)` | Create a new line consuming an unused Line resource. |
| `ExtendLineFromTerminal(LineId, TerminalStationId, ToStationId)` | Extend an existing line from a terminal station to a new station. |
| `ExtendLineInBetween(LineId, FromStationId, NewStationId, ToStationId)` | Insert a new station between two consecutive stations on an existing line. |
| `AddVehicleToLine(VehicleId, LineId, StationId)` | Deploy a train on a line at a station. |
| `RemoveLine(LineId)` | Remove an entire line and release its resource + all trains on it. |
| `RemoveVehicle(VehicleId)` | Remove a single train and release its resource. |

### RemoveVehicle Behavior

When a player returns `RemoveVehicle(VehicleId)`:

1. **No passengers on board** — the train is flagged `PendingRemoval` and removed in the same tick by the post-action finalization step (step 8b). The Train resource is released and `OnVehicleRemoved` fires.
2. **Passengers on board** — the train is flagged `PendingRemoval`. It continues moving and dropping off passengers but **will NOT pick up new passengers**. At each station it visits, any remaining passenger is force-dropped even if the station type doesn't match the passenger's destination (no points for a wrong-station drop). Once all passengers have been dropped, the finalization step (step 5b) removes the train, releases the resource, and fires `OnVehicleRemoved`.

> **Key detail:** A pending-removal train that reaches a station matching a passenger's destination type still delivers normally and scores a point. Force-drop only applies when no normal delivery is possible.

### RemoveVehicle Error Codes

| Code | Name | Cause |
|:-:|------|-------|
| 300 | `RemoveVehicleNotFound` | No active train with the given `VehicleId`. |
| 301 | `RemoveVehicleAlreadyPending` | The train is already flagged for removal. |

### RemoveLine Behavior

When a player returns `RemoveLine(LineId)`:

1. **The line is flagged `PendingRemoval`.**
2. **All trains currently assigned to the line are also flagged `PendingRemoval`** (unless they are already flagged — e.g. from a prior `RemoveVehicle` action).
3. Each pending-removal train follows the same drop-off-then-remove logic described under [RemoveVehicle Behavior](#removevehicle-behavior): it stops picking up new passengers, drops remaining passengers at the next station(s), and is removed once empty.
4. **After the last train on the line has been removed**, the finalization step (5b or 8b) removes the line itself, releases the Line resource (marks `InUse = false`), and fires `OnLineRemoved`.
5. **If the line has no trains at all**, it is flagged and removed in the same tick by the post-action finalization step (step 8b).

> **Event order:** When a line with trains is removed in the same finalization pass, `OnVehicleRemoved` fires **before** `OnLineRemoved` for each train.

### RemoveLine Error Codes

| Code | Name | Cause |
|:-:|------|-------|
| 400 | `RemoveLineNotFound` | No active line with the given `LineId`. |
| 401 | `RemoveLineAlreadyPending` | The line is already flagged for removal. |

### Invalid Action Handling

If an action is invalid, the engine:
1. Calls `OnInvalidPlayerAction(snapshot, errorCode, description)`.
2. Records the action as `NoAction` in the snapshot (no visual effect).
3. The game state is **unchanged** — the invalid action has no effect.

---

## 7. Lines

Lines are the routes that trains follow. They connect a sequence of stations.

### Rules

| Rule | Detail |
|------|--------|
| A line connects 2+ stations | Created with exactly 2 stations via `CreateLine`, extended one station at a time via `ExtendLineFromTerminal`. |
| Creating a line consumes a Line resource | The resource must exist, be unused, and of type `Line`. |
| Lines can be extended | Use `ExtendLineFromTerminal` with the `LineId` of an existing line to add a station to either end. |
| Stations can be inserted mid-line | Use `ExtendLineInBetween` to insert a new station between two consecutive stops. `FromStationId` and `ToStationId` must be adjacent on the line (in either order). Trains on the line have their path indices recalculated after insertion. |
| Extension must be from a terminal | `TerminalStationId` must be the first or last station on the line. |
| No duplicate stations on a line | `ToStationId` / `NewStationId` cannot already appear on the line (no loops). |
| Lines have a color | Assigned sequentially by creation order from a palette of 8 colors (see [Visual Reference](#18-visual-reference)). |
| Line path follows the grid | The tile path between stations uses straight + 45° diagonal segments. |
| Pending removal | When `RemoveLine` is applied, the line is flagged `PendingRemoval`. It cannot receive new trains or be extended. The line is physically removed once all its trains have been removed. See [RemoveLine Behavior](#removeline-behavior). |

### CreateLine Error Codes

| Code | Name | Cause |
|:-:|------|-------|
| 100 | `LineResourceNotFound` | No Line resource with the given `LineId`. |
| 101 | `LineResourceAlreadyInUse` | The Line resource is already deployed (use `ExtendLineFromTerminal` instead). |
| 102 | `LineStationsSameStation` | `FromStationId == ToStationId`. |
| 103 | `LineSegmentAlreadyExists` | The two stations are already directly connected. |

### ExtendLineFromTerminal Error Codes

| Code | Name | Cause |
|:-:|------|-------|
| 104 | `LineExtendLineNotFound` | No active line with the given `LineId`. |
| 105 | `LineExtendFromNotTerminal` | `TerminalStationId` is not at either end of the line. |
| 106 | `LineExtendToAlreadyOnLine` | `ToStationId` is already on this line. |

### ExtendLineInBetween Error Codes

| Code | Name | Cause |
|:-:|------|-------|
| 107 | `LineInsertLineNotFound` | No active line with the given `LineId`. |
| 108 | `LineInsertStationsNotConsecutive` | `FromStationId` and `ToStationId` are not adjacent stops on the line. |
| 109 | `LineInsertStationAlreadyOnLine` | `NewStationId` is already on this line. |
| 110 | `LineInsertStationNotSpawned` | `NewStationId` does not match any spawned station. |

### Line Path Computation

The path between two consecutive stations on a line is computed as follows:

1. **Determine the dominant axis**: horizontal (`|dx| ≥ |dy|`) or vertical.
2. **Split into segments**: a straight portion, a 45° diagonal, and another straight portion.
3. **Expand to tiles**: each segment is broken into single-tile steps (horizontal, vertical, or diagonal).

```
Station A ────straight────╲
                            ╲ diagonal (45°)
                              ╲────straight──── Station B
```

> The path is symmetric: the straight portions before and after the diagonal are equal in length.

---

## 8. Trains

Trains are vehicles that travel along lines, picking up and delivering passengers.

### Rules

| Rule | Detail |
|------|--------|
| Deploying a train consumes a Train resource | The resource must exist, be unused, and of type `Train`. |
| Trains spawn at a station on the line | `StationId` must be a station on the target line. |
| Trains start with Direction = +1 | Moving toward the end of the line path (higher indices). |
| Trains have a vehicle capacity | Default: 6 passengers. Configurable per level via `VehicleCapacity`. |
| Max trains per line = number of stations | A 3-station line can have at most 3 trains. |
| Spawn tile must be unoccupied | Cannot deploy a train on a tile already occupied by another train. |
| Pending removal | When `PendingRemoval` is `true`, the train continues moving and dropping off passengers but will NOT board new ones. Once empty, the train is removed and the Train resource is released for redeployment. |

### AddVehicleToLine Error Codes

| Code | Name | Cause |
|:-:|------|-------|
| 200 | `TrainResourceNotFound` | No unused Train resource with the given `VehicleId`. |
| 201 | `TrainLineNotFound` | The target line doesn't exist. |
| 202 | `TrainStationNotOnLine` | The spawn station is not part of the target line. |
| 203 | `TrainStationNotSpawned` | The spawn station hasn't appeared on the map yet. |
| 204 | `TrainLineAtCapacity` | The line already has max trains (= number of stations). |
| 205 | `TrainTileOccupied` | Another train is on the requested spawn tile. |

---

## 9. Train Movement

Trains move **one tile per hour** along their line's computed tile path, bouncing back and forth between terminals.

### Rules

| Rule | Detail |
|------|--------|
| Speed = 1 tile per tick | Every non-working tick, a train advances to the next tile on its path. |
| Direction: +1 or -1 | `+1` moves toward the end of the path, `-1` toward the start. |
| Terminal reversal | When a train reaches either end of the path, it reverses direction. |
| Single-tile paths = idle | If a line's path has only 1 tile, the train cannot move. |
| Trains stay at stations while working | Pickup or drop-off keeps the train at the station tile for that tick. |
| Diagonal movement is allowed | Tiles can be adjacent horizontally, vertically, or at 45°. Each step is still 1 tick. |

### Movement Example

A train on a 6-tile path from `(0,0)` to `(5,0)`:

```
Tick 0:  [T][ ][ ][ ][ ][ ]  → Train at (0,0), Direction +1
Tick 1:  [ ][T][ ][ ][ ][ ]  → Moves to (1,0)
Tick 2:  [ ][ ][T][ ][ ][ ]  → Moves to (2,0)
Tick 3:  [ ][ ][ ][T][ ][ ]  → Moves to (3,0)
Tick 4:  [ ][ ][ ][ ][T][ ]  → Moves to (4,0)
Tick 5:  [ ][ ][ ][ ][ ][T]  → Reaches terminal (5,0)
Tick 6:  [ ][ ][ ][ ][T][ ]  → Reverses, Direction -1
Tick 7:  [ ][ ][ ][T][ ][ ]  → Moves to (3,0)
  ⋮
Tick 10: [T][ ][ ][ ][ ][ ]  → Back at start, full round trip
```

---

## 10. Passenger Pickup & Drop-off

Train movement uses a **three-phase pipeline** each tick to handle all trains simultaneously.

### Phase 1: Decision Making

Each train independently decides what to do. All decisions read from the **same starting snapshot** (simultaneous).

At a station, the train checks in this **priority order**:

```
┌─────────────────────────────────────────────────────┐
│            TRAIN AT A STATION — PRIORITY            │
├─────────────────────────────────────────────────────┤
│  1. Deliver: Drop a passenger whose destination     │
│     type matches this station's type.               │
│                                                     │
│  1b.Force-drop (pending removal only): If the train │
│     is flagged PendingRemoval, drop the first        │
│     remaining passenger here even if the station     │
│     type doesn't match (no points — re-queued).     │
│                                                     │
│  2. Transfer: Drop a passenger who can reach their  │
│     destination faster via another line from here.   │
│                                                     │
│  3. Pickup: Board a waiting passenger if this train │
│     is on the optimal route to their destination.   │
│     (Skipped when PendingRemoval is true.)          │
│                                                     │
│  4. Move: If no work to do, advance to next tile.   │
└─────────────────────────────────────────────────────┘
```

### Pickup Rules

| Rule | Detail |
|------|--------|
| Passenger must be at this station | `passenger.StationId == currentStation.Id`. |
| Train must have capacity | `train.Passengers.Count < VehicleCapacity`. |
| Destination must be reachable | The global shortest path (Dijkstra) to the destination type must not be `∞`. |
| Train must be on optimal route | The next station on this line must be on a globally shortest path to the destination. Formally: `stepsToNext + shortestFromNext == shortestFromHere`. |
| Oldest passenger first | When multiple passengers qualify, the one with the lowest `SpawnedAtHour` is picked. |
| One operation per tick | A train picks up **at most one** passenger per tick. |

### Drop-off Rules

| Rule | Detail |
|------|--------|
| Final delivery | If the station's type matches the passenger's `DestinationType`, the passenger is removed and a point is scored. |
| Intermediate transfer | If the station type does NOT match, the passenger is **re-queued** at this station (no point). Another train can pick them up later. |
| Force-drop (pending removal) | A `PendingRemoval` train force-drops its first remaining passenger at any station, even if the type doesn't match. The passenger is re-queued (no point). Normal delivery (step 1) still takes precedence — if the station type matches, the passenger is delivered normally with a point. |
| One drop per tick | A train drops **at most one** passenger per tick (prioritizing delivery over transfer). |

---

## 11. Routing & Transfers

The engine uses **Dijkstra's shortest-path algorithm** to make intelligent passenger routing decisions.

### Rules

| Rule | Detail |
|------|--------|
| Graph = station adjacency | Stations connected by lines form an undirected weighted graph. |
| Edge weight = Chebyshev distance | `cost = max(\|Ax - Bx\|, \|Ay - By\|)` — equals the exact tile steps between stations. |
| Free transfers | Switching between lines at a shared station costs 0 extra steps. |
| Unreachable destinations | If Dijkstra returns `∞`, the passenger cannot be delivered and won't be picked up. |
| Dijkstra cache | Results are cached per tick as `(stationId, destType) → steps` to avoid redundant computation. |

### Transfer Drop Logic

A carried passenger is dropped for transfer when **all** of these are true:
1. A shorter path exists via another line from the current station (`overall < viaLine`).
2. The **next** station on this train's route is NOT on the globally optimal path (otherwise, keep riding to the interchange).

### Via-Line Calculation

The engine checks how many tile-steps the passenger would need if they stayed on this train:
- **Forward**: Count steps to the first matching-type station in the direction of travel.
- **Backward**: Count steps to the terminal, then back to the first matching-type station on the other side.
- Take the minimum.

---

## 12. Collision Rules

After all trains decide their desired actions (Phase 1), the engine resolves collisions (Phase 2) before applying results (Phase 3).

### Three Collision Rules

```
┌─────────────────────────────────────────────────────────────────┐
│  Rule A: STATION OCCUPATION (direction-agnostic)                │
│  Only one train per station tile at a time.                     │
│  A train trying to enter an occupied station is BLOCKED.        │
├─────────────────────────────────────────────────────────────────┤
│  Rule B: SAME-DIRECTION ON OPEN TRACK                          │
│  On non-station tiles, only same-direction trains block.        │
│  Opposite-direction trains may CROSS FREELY.                    │
├─────────────────────────────────────────────────────────────────┤
│  Rule C: SIMULTANEOUS STATION ARRIVAL                          │
│  When two moving trains target the same station tile,           │
│  the train with the LOWER INDEX wins; the other is blocked.    │
└─────────────────────────────────────────────────────────────────┘
```

### Cascade Blocking

Collision resolution iterates to a **fixed point**: a newly blocked train can itself block trains behind it in subsequent passes. This continues until no more trains are affected.

### Blocked Behavior

When a train is blocked, it stays at its **current** tile for this tick — position, direction, and path index are all unchanged.

---

## 13. Scoring

| Rule | Detail |
|------|--------|
| +1 point per delivery | A point is scored when a passenger is dropped at a station whose type matches their `DestinationType`. |
| No points for transfers | Dropping a passenger at a non-matching station (intermediate transfer) awards 0 points. |
| Score is cumulative | The score carries forward across all ticks. |
| Scoring happens in Phase 3 | After collision resolution, during the apply step. |
| Final score = last snapshot's score | The simulation result's `TotalScore` equals the score in the final snapshot. |

---

## 14. Station Crowding & Game Over

The engine checks station crowding **every tick**, after `OnDayStart` but **before** new stations and passengers spawn.

### Thresholds

```
                   0        10        20
Passengers    ─────┼─────────┼─────────┼──►
                   │         │         │
                   │  Normal │ Crowded │ GAME OVER
                   │         │         │
                   │         ▼         ▼
                   │    OnStationCrowded  OnGameOver
                   │    (warning only)    (simulation ends)
```

| Threshold | Value | Effect |
|:-:|:-:|---|
| Crowded | **≥ 10** passengers | `OnStationCrowded(snapshot, stationId, count)` fires. **Warning only** — game continues. |
| Game Over | **≥ 20** passengers | `OnGameOver(snapshot, stationId)` fires. **Simulation immediately stops.** |

### Rules

| Rule | Detail |
|------|--------|
| Checked every tick | Before station/passenger spawning so the runner sees what caused the crowd. |
| Crowded fires every tick | As long as a station has ≥ 10 passengers, `OnStationCrowded` fires each tick. |
| Game over fires once | The simulation breaks after the first station hits ≥ 20. |
| No crowded event on game-over tick | When a station triggers game over, `OnStationCrowded` does NOT fire for it. |
| Game over saves the final snapshot | The current snapshot is appended to history before breaking. |
| Count = passengers waiting at station | `snapshot.Passengers.Count(p => p.StationId == station.Id)` |

---

## 15. Event Callbacks

The `IMetroManiaRunner` interface defines all callbacks that the player bot implements:

| Callback | When | Returns |
|----------|------|---------|
| `OnDayStart(snapshot)` | Hour 0 of each day | `void` |
| `OnStationSpawned(snapshot, stationId, location, stationType)` | When a station appears on the map | `void` |
| `OnPassengerSpawned(snapshot, stationId, passengerId)` | When a passenger appears at a station | `void` |
| `OnWeeklyGiftReceived(snapshot, gift)` | Monday at Hour 0 | `void` |
| `OnStationCrowded(snapshot, stationId, count)` | Each tick a station has ≥ 10 passengers | `void` |
| `OnGameOver(snapshot, stationId)` | When a station reaches ≥ 20 passengers | `void` |
| `OnHourTicked(snapshot)` | Every tick (always last event) | `PlayerAction` |
| `OnInvalidPlayerAction(snapshot, code, description)` | When the returned action was rejected | `void` |
| `OnVehicleRemoved(snapshot, vehicleId)` | When a pending-removal train has been fully removed | `void` |
| `OnLineRemoved(snapshot, lineId)` | When a pending-removal line has had all trains removed and is itself removed | `void` |

### Callback Ordering Within a Tick

```
  Hour 0 of a day?  ──yes──►  OnDayStart
         │
         ▼
  Any station ≥ 20? ──yes──►  OnGameOver → END
         │ no
         ▼
  Any station ≥ 10? ──yes──►  OnStationCrowded (for each)
         │
         ▼
  Stations to spawn? ──yes──►  OnStationSpawned (for each)
         │
         ▼
  Passengers to spawn? ──yes──►  OnPassengerSpawned (for each)
         │
         ▼
  Process trains (3-phase pipeline)
         │
         ▼
  Pending removals with 0 passengers?
         │──yes──►  Remove train, release resource
         │          OnVehicleRemoved (for each)
         │          Lines with PendingRemoval + no trains left?
         │          ──yes──►  Remove line, release resource
         │                    OnLineRemoved (for each)
         ▼
  Monday at 00:00?  ──yes──►  OnWeeklyGiftReceived
         │
         ▼
  OnHourTicked → Player returns action
         │
         ▼
  Apply player action
         │
  Invalid? ──yes──►  OnInvalidPlayerAction
         │
         ▼
  Pending removals with 0 passengers?
         │──yes──►  Remove train, release resource
         │          OnVehicleRemoved (for each)
         │          Lines with PendingRemoval + no trains left?
         │          ──yes──►  Remove line, release resource
         │                    OnLineRemoved (for each)
         ▼
  Record snapshot
```

---

## 16. Game Snapshot

The `GameSnapshot` record represents the complete game state at a single point in time. A new snapshot is recorded every tick.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Time` | `GameTime` | Current day, hour, and day of week. |
| `TotalHoursElapsed` | `int` | 0-indexed tick counter. |
| `Score` | `int` | Cumulative delivery points. |
| `Resources` | `IReadOnlyList<Resource>` | All Line and Train resources (used and unused). |
| `Stations` | `Dictionary<Location, Station>` | All spawned stations, keyed by grid location. |
| `Lines` | `IReadOnlyList<Line>` | All deployed metro lines. |
| `Trains` | `IReadOnlyList<Train>` | All active trains with position and carried passengers. |
| `Passengers` | `IReadOnlyList<Passenger>` | All passengers waiting at stations (not on trains). |
| `LastAction` | `PlayerAction?` | The action executed this tick (or `NoAction`). |

### Key Structures

**`GameTime`** — `readonly record struct (Day, Hour, DayOfWeek)`

**`Location`** — `record struct (X, Y)` — integer grid coordinates.

**`Station`** — `record { Id, Location, StationType }`

**`Line`** — `record { LineId, OrderId, StationIds }`

**`Train`** — `record { TrainId, LineId, TilePosition, Direction, PathIndex, Passengers }`

**`Passenger`** — `record (DestinationType, SpawnedAtHour) { Id, StationId }`
- `StationId` is `null` when the passenger is riding a train.

**`Resource`** — `record { Id, Type, InUse }`

---

## 17. Determinism

The simulation is **fully deterministic**: the same level configuration and bot implementation always produce identical results.

### Rules

| Rule | Detail |
|------|--------|
| All randomness is seeded | The level's `Seed` value drives all RNG (passenger destinations). |
| Per-station RNG isolation | Each station has its own RNG seed per tick. Adding/removing stations elsewhere doesn't affect other stations' passenger destinations. |
| No external state | The engine holds no mutable state between invocations. |
| Immutable snapshot history | Each tick produces a fresh shallow copy. Mutations never retroactively alter stored snapshots. |
| Reproducible across runs | Running the same level with the same bot twice produces identical snapshot sequences. |

---

## 18. Visual Reference

### Grid

- **Tile size**: 32 × 32 pixels.
- **Coordinate system**: `(0, 0)` = top-left. X increases rightward, Y increases downward.
- **Pixel center of a tile**: `(X × 32 + 16, Y × 32 + 16)`.

### Line Colors

Lines are assigned colors from this palette in creation order:

| Order | Color | Hex |
|:-:|:-:|:-:|
| 1 | 🔴 Red | `#E53935` |
| 2 | 🔵 Blue | `#1976D2` |
| 3 | 🟢 Green | `#388E3C` |
| 4 | 🟠 Orange | `#F57F17` |
| 5 | 🟣 Purple | `#71209B` |
| 6 | 🩵 Teal | `#0097A7` |
| 7 | 🩷 Pink | `#AD1457` |
| 8 | 🩶 Slate | `#37474F` |

> If more than 8 lines are created, colors cycle back to the start.

### Train Appearance

- **Shape**: Pointy-front rectangle with rounded rear corners (22px × 12px).
- **Fill**: Line color. **Stroke**: White, 1.5px.
- **Rotation**: Oriented toward the next waypoint on the path.
- **Passengers inside**: Tiny destination-shape icons (3px) arranged in a grid inside the body.

### Passenger Icons

- **Waiting at station**: Dark gray (`#333333`) destination-shape icons (5px), drawn above the station.
- **Riding a train**: White destination-shape icons (3px), drawn inside the train body.
- **Gap between icons**: 1.5px.

### Rendering Layers (front to back)

```
5. Trains          (topmost)
4. Waiting passengers
3. Station icons
2. Metro lines
1. Water tiles
0. Background      (bottommost)
```

### Water Tiles

Water tiles use 8-directional neighbor analysis. Each tile's SVG is selected based on which of its 8 neighbors (N, NE, E, SE, S, SW, W, NW) are also water or out of bounds. Grid boundaries are treated as water for seamless edge blending.

> **Diagonal rule**: A diagonal direction (e.g., NE) is only included if **both** adjacent cardinals (N and E) are also water.
