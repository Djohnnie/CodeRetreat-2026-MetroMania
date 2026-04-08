# Metro Mania — Player Manual

## Welcome to the Challenge

**Metro Mania** is a coding challenge where you write a C# bot to manage a growing metro network. Your bot listens to game events, plans routes, and issues one command every hour to keep passengers flowing across the city. The game ends when too many passengers pile up at a single station — so keep that network moving!

## The Game World

The simulation takes place on a tile-based grid. As the game progresses, new stations appear at various locations across the map. Water tiles may fill parts of the grid but do not block metro lines — lines run conceptually above the terrain.

### Station Types

Every station has a **shape**. Passengers spawn at stations and want to reach *any* station of their matching shape. There are six types:

| Shape | Name |
|-------|------|
| ● | Circle |
| ■ | Rectangle |
| ▲ | Triangle |
| ◆ | Diamond |
| ⬠ | Pentagon |
| ✦ | Star |

## Passengers

Passengers appear at stations over time according to each station's spawn schedule. A passenger's goal is to reach **any** station of the correct shape — not one specific station.

Passengers are smart: they only board a train if that train's line actually leads toward a station of their destination type. A dead-end line connecting unrelated stations will not attract boarders.

> **Warning:** When a station accumulates **10 or more** waiting passengers, your `OnStationOverrun` callback fires. Act fast!

> **Game Over:** When any station reaches **20 or more** waiting passengers, the game immediately ends.

## Resources

You start each run with a limited pool of resources:

- **1 Line** — defines a metro route between stations
- **1 Train** — a vehicle to place on a line

Every **Monday at midnight** (the start of each in-game week), the engine checks for a **weekly gift override** defined by the level designer. If one exists, your bot receives that resource via `OnWeeklyGiftReceived`. If no override is defined for that week, no gift is awarded.

| Resource | Purpose |
|----------|---------|
| Line | Define a route connecting two or more stations |
| Train | A vehicle that shuttles back and forth along a line |

Each train holds **6 passengers** by default (configurable per level).

## Time

The simulation advances **hour by hour** (24 hours = 1 day). Each call to `OnHourTick` represents one in-game hour. A level typically runs for up to 200 days, or until a game-over event occurs.

## Your Bot — The IMetroManiaRunner Interface

Your bot is a C# class that implements `IMetroManiaRunner`. The game engine calls these methods as events occur during the simulation:

### Event Callbacks

| Method | When it fires | Returns |
|--------|--------------|---------|
| `OnHourTick(snapshot)` | Every hour | `PlayerAction` |
| `OnDayStart(snapshot)` | At midnight each day | — |
| `OnWeeklyGift(snapshot, gift)` | Monday at midnight (only if override defined) | — |
| `OnStationSpawned(snapshot, id, location, type)` | A new station appears | — |
| `OnPassengerWaiting(snapshot, location, passengers)` | A passenger begins waiting | — |
| `OnStationOverrun(snapshot, location, passengers)` | 10+ passengers at a station | — |
| `OnGameOver(snapshot, location, passengers)` | 20+ passengers — game ends | — |

> **`OnHourTick` is your main control loop.** It is the only callback that returns an action. All other callbacks are informational — use them to track state and plan ahead.

### The GameSnapshot

Every callback receives a `GameSnapshot` containing the complete current state:

- `snapshot.Stations` — all spawned stations (id, location, type, passenger count)
- `snapshot.Lines` — all active lines (id, ordered list of station IDs)
- `snapshot.Vehicles` — all vehicles (id, line, passengers aboard, current position)
- `snapshot.Resources.AvailableLines` — line resources not yet placed
- `snapshot.Resources.AvailableVehicles` — trains not yet placed
- `snapshot.GameTime` — current `Day` and `Hour`

---

## Player Actions

From `OnHourTick`, return exactly **one action** per tick. Only one operation can be performed per hour — choose wisely.

### `CreateLine` — Start a New Route

```csharp
return new CreateLine(
    LineId: Guid.NewGuid(),
    StationIds: [stationA, stationB]);
```

Creates a new metro line connecting two or more stations in order. Consumes one available **Line** resource. The line can be extended later.

### `ExtendLine` — Add a Station to a Line

```csharp
return new ExtendLine(
    LineId: existingLineId,
    FromStationId: currentEndStation,
    ToStationId: newStation);
```

Appends a station to the front or back of an existing line. `FromStationId` must be the first or last station on the line.

### `InsertStationInLine` — Insert a Station Mid-Route

```csharp
return new InsertStationInLine(
    LineId: existingLineId,
    NewStationId: stationToInsert,
    FromStationId: adjacentStationA,
    ToStationId: adjacentStationB);
```

Inserts a new station between two stations that are already adjacent on the line.

### `RemoveLine` — Delete a Route

```csharp
return new RemoveLine(LineId: lineId);
```

Removes an entire line and returns all its resources (line token + all vehicles on it) to your available pool.

### `AddVehicleToLine` — Deploy a Train

```csharp
return new AddVehicleToLine(
    VehicleId: availableVehicleId,
    LineId: targetLineId,
    StationId: startingStation);
```

Places an available train onto a line at the given starting station. The train begins shuttling immediately.

### `RemoveVehicle` — Recall a Train

```csharp
return new RemoveVehicle(VehicleId: vehicleId);
```

Removes a vehicle from its line and returns it to your available pool.

### `NoAction`— Skip This Tick

```csharp
return PlayerAction.None;  // or: return new NoAction();
```

---

## Starter Code

Your submission begins from this template:

```csharp
public class MyMetroManiaRunner : IMetroManiaRunner
{
    public PlayerAction OnHourTick(GameSnapshot snapshot) => PlayerAction.None;

    public void OnDayStart(GameSnapshot snapshot) { }

    public void OnWeeklyGift(GameSnapshot snapshot, ResourceType gift) { }

    public void OnStationSpawned(GameSnapshot snapshot, Guid stationId,
        Location location, StationType stationType) { }

    public void OnPassengerWaiting(GameSnapshot snapshot, Location location,
        IReadOnlyList<Passenger> passengers) { }

    public void OnStationOverrun(GameSnapshot snapshot, Location location,
        IReadOnlyList<Passenger> passengers) { }

    public void OnGameOver(GameSnapshot snapshot, Location location,
        IReadOnlyList<Passenger> passengers) { }
}
```

---

## Scoring

Your score increases each time a passenger is successfully delivered to a station of their destination type. Each level runs for a fixed number of days. The **longer you survive** and **the more passengers you deliver**, the higher your score. Your **best score per level** counts toward the leaderboard.

---

## Strategy Tips

- **Save station IDs in `OnStationSpawned`** — store them in class fields so you can act on new stations in the next `OnHourTick`.
- **React immediately to `OnStationOverrun`** — at 10 passengers you have roughly 10 more hours before game over.
- **Connect every station** — an unlinked station accumulates passengers with no escape route. Always prioritize new stations.
- **Think about line topology** — hub-and-spoke or loop networks typically outperform a single long chain.
- **Use gifts immediately** — when you receive a resource via `OnWeeklyGiftReceived`, try to deploy it the same tick.
- **Passenger routing is intelligent** — passengers only board when the line leads toward their destination type. Randomly connecting stations does not help.
- **Remove and rebuild** — dismantling a poorly planned line and rebuilding it with a better route is often worth the temporary disruption.
