# MetroMania Game Model Reference

This document describes every type your bot interacts with. The engine passes these objects to your `IMetroManiaRunner` callbacks and expects `PlayerAction` return values. All types are **immutable records** — you read them but never mutate them.

---

## Table of Contents

1. [Enums](#enums) — `StationType`, `ResourceType`
2. [Core Value Types](#core-value-types) — `Location`, `GameTime`
3. [Game Entities](#game-entities) — `Station`, `Passenger`, `Resource`, `Line`, `Train`
4. [Game State](#game-state) — `GameSnapshot`
5. [Player Actions](#player-actions) — `PlayerAction` and its subtypes
6. [Runner Contract](#runner-contract) — `IMetroManiaRunner`
7. [Error Codes](#error-codes) — `PlayerActionError`

---

## Enums

### StationType

Each station on the map has a shape. Passengers want to travel to a station whose shape matches their `DestinationType`.

<!-- CODE:src/MetroMania.Domain/Enums/StationType.cs -->

### ResourceType

The two kinds of deployable resources the player receives throughout the game.

<!-- CODE:src/MetroMania.Domain/Enums/ResourceType.cs -->

---

## Core Value Types

### Location

A position on the game grid expressed as integer tile coordinates.

<!-- CODE:src/MetroMania.Engine/Model/Location.cs#Location -->

| Parameter | Description |
|-----------|-------------|
| `X` | Horizontal tile coordinate (column). |
| `Y` | Vertical tile coordinate (row). |

### GameTime

The current point in game time, provided in every `GameSnapshot`.

<!-- CODE:src/MetroMania.Engine/Model/GameTime.cs -->

| Parameter | Description |
|-----------|-------------|
| `Day` | 1-indexed day number (day 1 is the first day). |
| `Hour` | Hour within the day (0–23). |
| `DayOfWeek` | The .NET `DayOfWeek` value (Sunday = 0 … Saturday = 6). |

---

## Game Entities

### Station

A metro station placed on the game grid. Stations spawn over time; your bot is notified via `OnStationSpawned`.

<!-- CODE:src/MetroMania.Engine/Model/Station.cs -->

| Property | Description |
|----------|-------------|
| `Id` | Unique identifier for this station. |
| `Location` | Grid coordinates where the station is placed. |
| `StationType` | The shape of the station — determines which passengers it can serve. |

### Passenger

A passenger waiting at a station or riding a train. Passengers want to reach a station whose shape matches their `DestinationType`. They are delivered (and score a point) when a train drops them at such a station.

<!-- CODE:src/MetroMania.Engine/Model/Passenger.cs -->

| Property | Description |
|----------|-------------|
| `DestinationType` | The station shape this passenger wants to reach. |
| `SpawnedAtHour` | The total elapsed hour at which this passenger appeared. |
| `Id` | Unique identifier for this passenger. |
| `StationId` | The station where the passenger is waiting, or `null` if they are aboard a train. |

### Resource

A deployable resource (line or train) available to the player. You start with initial resources and receive more via weekly gifts.

<!-- CODE:src/MetroMania.Engine/Model/Resource.cs -->

| Property | Description |
|----------|-------------|
| `Id` | Unique identifier for this resource. Use this Id when creating lines or adding vehicles. |
| `Type` | `Line` or `Train`. |
| `InUse` | `true` if currently deployed on the map; `false` if available for use. |

### Line

A metro line connecting a sequence of stations. Trains travel back and forth along the line's station order.

<!-- CODE:src/MetroMania.Engine/Model/Line.cs -->

| Property | Description |
|----------|-------------|
| `LineId` | Unique identifier matching the consumed line resource Id. |
| `OrderId` | Display order index used for consistent colors and visual ordering. |
| `StationIds` | Ordered list of station Ids from the first terminal to the last. |
| `PendingRemoval` | When `true`, the line is scheduled for removal. All its trains are also flagged for pending removal. Once every train has dropped off its passengers and been removed, the line itself is removed and its resource released. |

### Train

A train (vehicle) moving along a metro line, picking up and dropping off passengers automatically.

<!-- CODE:src/MetroMania.Engine/Model/Train.cs -->

| Property | Description |
|----------|-------------|
| `TrainId` | Unique identifier copied from the consumed train resource Id. |
| `LineId` | The line this train is assigned to. |
| `TilePosition` | Current tile the train occupies (grid coordinates). |
| `Direction` | `+1` = moving toward the end of the line path; `-1` = moving toward the start. Reverses at terminals. |
| `PathIndex` | Index of the train's position within the line's computed tile path. `-1` means not yet initialized. |
| `Passengers` | Passengers currently riding this train. |
| `PendingRemoval` | When `true`, the train will continue dropping off passengers but will **not** pick up new ones. Once empty, the train is removed and its resource released. |

---

## Game State

### GameSnapshot

An immutable snapshot of the entire game state at a specific point in time. Every `IMetroManiaRunner` callback receives one of these as its first parameter.

<!-- CODE:src/MetroMania.Engine/Model/GameSnapshot.cs -->

| Property | Description |
|----------|-------------|
| `Time` | Current in-game time (day, hour, day of week). |
| `TotalHoursElapsed` | Total simulation hours elapsed since the start. |
| `Score` | The player's current cumulative score. |
| `Resources` | All resources (lines and trains) the player owns — both available and deployed. |
| `Stations` | All stations currently on the map, keyed by their grid `Location`. |
| `Lines` | All active metro lines on the map. |
| `Trains` | All active trains, including those with pending removal. |
| `Passengers` | All passengers in the game — waiting at stations or riding trains. |
| `NextLineOrderId` | The order identifier that will be assigned to the next line created. |
| `LastAction` | The last player action that was executed, or `null` if none yet. |

---

## Player Actions

Your bot returns a `PlayerAction` from `OnHourTicked` to tell the engine what to do. Only **one action per tick** is allowed. Return `PlayerAction.None` to skip a tick.

### PlayerAction (base)

<!-- CODE:src/MetroMania.Engine/Model/PlayerAction.cs#PlayerAction -->

### NoAction

Do nothing this tick.

<!-- CODE:src/MetroMania.Engine/Model/PlayerAction.cs#NoAction -->

### CreateLine

Create a new metro line by consuming an available line resource and connecting two stations.

<!-- CODE:src/MetroMania.Engine/Model/PlayerAction.cs#CreateLine -->

| Parameter | Description |
|-----------|-------------|
| `LineId` | Id of an available (not in-use) `Line` resource. |
| `FromStationId` | Id of the first station. |
| `ToStationId` | Id of the second station (must differ from `FromStationId`). |

### ExtendLineFromTerminal

Extend an existing line from one of its terminal (first or last) stations to a new station.

<!-- CODE:src/MetroMania.Engine/Model/PlayerAction.cs#ExtendLineFromTerminal -->

| Parameter | Description |
|-----------|-------------|
| `LineId` | Id of an existing (in-use) line. |
| `TerminalStationId` | Must be the first or last station on the line. |
| `ToStationId` | The new station to add — must not already be on the line. |

### ExtendLineInBetween

Insert a station between two consecutive stations on an existing line.

<!-- CODE:src/MetroMania.Engine/Model/PlayerAction.cs#ExtendLineInBetween -->

| Parameter | Description |
|-----------|-------------|
| `LineId` | Id of an existing line. |
| `FromStationId` | One of two consecutive stations on the line. |
| `NewStationId` | A spawned station not already on the line — will be inserted between the two. |
| `ToStationId` | The other consecutive station (order does not matter). |

### RemoveLine

Remove an entire metro line and release its resource along with all vehicle resources on it.

<!-- CODE:src/MetroMania.Engine/Model/PlayerAction.cs#RemoveLine -->

| Parameter | Description |
|-----------|-------------|
| `LineId` | Id of an active (in-use) line. |

> **Note:** Removal is not instant. The line and its trains are flagged as `PendingRemoval`. Trains will finish dropping off passengers before being removed. You are notified via `OnVehicleRemoved` and `OnLineRemoved` when the process completes.

### AddVehicleToLine

Deploy an available train resource onto an existing line at a specific station.

<!-- CODE:src/MetroMania.Engine/Model/PlayerAction.cs#AddVehicleToLine -->

| Parameter | Description |
|-----------|-------------|
| `VehicleId` | Id of an available (not in-use) `Train` resource. |
| `LineId` | Id of an existing line. |
| `StationId` | A station on the line where the train will spawn (tile must not be occupied by another train). |

### RemoveVehicle

Remove a train from its line and release the vehicle resource.

<!-- CODE:src/MetroMania.Engine/Model/PlayerAction.cs#RemoveVehicle -->

| Parameter | Description |
|-----------|-------------|
| `VehicleId` | Id of an active train on the map. |

> **Note:** Like line removal, vehicle removal is deferred. The train is flagged as `PendingRemoval` — it keeps moving and dropping off passengers but stops picking up new ones. Once empty, it is removed and you are notified via `OnVehicleRemoved`.

---

## Runner Contract

### IMetroManiaRunner

This is the interface your bot must implement. The engine calls these methods during the simulation.

<!-- CODE:src/MetroMania.Engine/Contracts/IMetroManiaRunner.cs -->

| Method | When It Is Called |
|--------|-------------------|
| `OnHourTicked` | At the end of every simulation hour. **Must return a `PlayerAction`** (return `PlayerAction.None` to skip). |
| `OnDayStart` | At the start of each new day. |
| `OnWeeklyGiftReceived` | Every Monday at hour 0 when a new resource is awarded. |
| `OnStationSpawned` | When a new station appears on the map. |
| `OnPassengerSpawned` | When a new passenger appears at a station. |
| `OnStationCrowded` | When a station reaches **10+ waiting passengers** — this is a warning. |
| `OnGameOver` | When a station reaches **20+ waiting passengers** — the game ends. |
| `OnInvalidPlayerAction` | When the action you returned from `OnHourTicked` was rejected. Check the `code` against `PlayerActionError`. |
| `OnVehicleRemoved` | After a train with pending removal has dropped off all passengers and been removed. The train resource is available again. |
| `OnLineRemoved` | After a line with pending removal has had all its trains removed. The line resource is available again. |

---

## Error Codes

### PlayerActionError

When your bot returns an invalid action from `OnHourTicked`, the engine calls `OnInvalidPlayerAction` with one of these error codes. Use them to debug your bot logic.

<!-- CODE:src/MetroMania.Engine/Contracts/PlayerActionError.cs -->

| Code | Constant | Meaning |
|------|----------|---------|
| **100** | `LineResourceNotFound` | No line resource with the given `LineId` exists. |
| **101** | `LineResourceAlreadyInUse` | The line resource is already deployed on the map. |
| **102** | `LineStationsSameStation` | `FromStationId` and `ToStationId` are identical. |
| **103** | `LineSegmentAlreadyExists` | The two stations are already directly connected on an existing line. |
| **104** | `LineExtendLineNotFound` | No active line with the given `LineId` exists. |
| **105** | `LineExtendFromNotTerminal` | `TerminalStationId` is not a terminal of the line. |
| **106** | `LineExtendToAlreadyOnLine` | `ToStationId` already appears on the line. |
| **107** | `LineInsertLineNotFound` | No active line with the given `LineId` exists. |
| **108** | `LineInsertStationsNotConsecutive` | `FromStationId` and `ToStationId` are not consecutive on the line. |
| **109** | `LineInsertStationAlreadyOnLine` | `NewStationId` already appears on the line. |
| **110** | `LineInsertStationNotSpawned` | `NewStationId` has not yet spawned on the map. |
| **200** | `TrainResourceNotFound` | No unused train resource with the given `VehicleId` exists. |
| **201** | `TrainLineNotFound` | The target line does not exist on the map. |
| **202** | `TrainStationNotOnLine` | The spawn station is not part of the target line. |
| **203** | `TrainStationNotSpawned` | The spawn station has not yet appeared on the map. |
| **204** | `TrainLineAtCapacity` | The line already has one train per station (maximum reached). |
| **205** | `TrainTileOccupied` | Another train is already on the requested spawn tile. |
| **300** | `RemoveVehicleNotFound` | No active train with the given `VehicleId` exists. |
| **301** | `RemoveVehicleAlreadyPending` | The train is already flagged for pending removal. |
| **400** | `RemoveLineNotFound` | No active line with the given `LineId` exists. |
| **401** | `RemoveLineAlreadyPending` | The line is already flagged for pending removal. |
