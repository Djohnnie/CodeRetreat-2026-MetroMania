---
name: metro-mania-engine
description: 'You are an expert in the MetroMania game engine codebase. Apply this knowledge whenever the user asks to build, extend, debug, or reason about the engine.'
---

# MetroMania Engine Skill

You are an expert in the MetroMania game engine codebase. Apply this knowledge whenever the user asks to build, extend, debug, or reason about the engine.

## Project Context

MetroMania is a coding-challenge web app inspired by Mini Metro. Players write C# bots (`IMetroManiaRunner`) to manage a metro network. The engine is a deterministic, tick-based simulation.

- **Solution:** `C:\_-_GITHUB_-_\CodeRetreat-2026-MetroMania`
- **Engine project:** `src/MetroMania.Engine/MetroMania.Engine.csproj`
- **Test project:** `src/MetroMania.Engine.Tests/` (Reqnroll + xUnit v3)
- **Build:** `dotnet build`
- **Tests:** `dotnet test src\MetroMania.Engine.Tests`

## GameState

The `GameState` class represents the current state of the game at any given time. It includes information about the current day, hour, stations, passengers, lines, and resources available to the player. The engine updates the `GameState` as the simulation progresses, and players can access this information in their bot logic to make informed decisions.

The `GameState` includes properties such as:
- `CurrentDay`: The current day in the simulation.
- `CurrentHour`: The current hour in the simulation.
- `Resources`: The resources available to the player, including trains and lines.
- `Stations`: A collection of all the stations currently on the map, including their locations and types.
- `Passengers`: A collection of all the passengers currently in the game, including their locations and destinations.
- `Lines`: A collection of all the metro lines currently in operation, including their routes and the trains assigned to them.

## What the IMetroManiaRunner bot interface provides

The `IMetroManiaRunner` interface allows players to implement their bot logic for managing the metro network. It provides the following key methods that the engine calls at specific points in the simulation:
- `OnDayStart(GameState gameState)`: Called at the start of each day (hour 0). Players can use this to plan their actions for the day based on the current game state.
- `OnStationSpawned(GameState gameState, Guid stationId, Location location, StationType stationType)`: Called whenever a new station is spawned at the end of the day. Players can use this to react to new stations appearing on the map.
- `OnPassengerSpawned(GameState gameState, Guid stationId, Guid passengerId)`: Called whenever a new passenger is spawned at a station. Players can use this to react to new passengers appearing and plan their routes accordingly.
- `OnWeeklyGiftReceived(GameState gameState, ResourceType resourceType)`: Called on Monday at Hour 0 when the level has a weekly gift override for that week. Only fires if the level designer has defined a gift for the current week — weeks without an override are silently skipped.
- `OnHourTicked(GameState gameState)`: Called at the end of every hour after all other events and movements have been processed. Players return a `PlayerAction` from this method, which the engine then applies to the game state.

## Player actions

In the `OnHourTick` method, players return a `PlayerAction` which can include the following types of actions:
- **None**: Do nothing this hour.
- **CreateLine**: Create a new metro line that is available in the resources between two spawned stations
- **RemoveLine**: Remove an existing metro line from the map and return it back to the available resources
- **AddTrainToLine**: Add a train to an existing metro line at a specific station, if the player has trains available in their resources
- **RemoveTrainFromLine**: Remove a train from an existing metro line and return it back to the available resources
- **ExtendLine**: Extend an existing metro line from one of its current terminal stations to a new station that becomes the new terminal

## Simulation flow

The simulation progresses in a deterministic, tick-based manner. Each day consists of 24 hours, and the engine processes events in the following order:
1. **Day Start**: The engine calls `OnDayStart` for the player's bot, allowing them to plan their actions for the day.
2. **Station Spawns**: At the end of the day, new stations may spawn on the map. The engine calls `OnStationSpawned` for each new station, allowing the player to react to the new stations.
3. **Passenger Spawns**: Whenever new passengers spawn at stations, the engine calls `OnPassengerSpawned` for each new passenger, allowing the player to react to the new passengers.
4. **Weekly Gift**: Every Monday, the engine checks if a `WeeklyGiftOverride` is defined for the current week. If so, the player receives that resource and the engine calls `OnWeeklyGiftReceived`. If no override exists, no gift is given.
5. **Hour Tick**: At the end of every hour, after all events and movements have been processed, the engine calls `OnHourTicked`, allowing the player to return a `PlayerAction` that will be applied to the game state.

The engine then updates the `GameState` based on the player's actions and the movements of passengers and trains, and the simulation continues to the next hour.

## Levels

Levels are grid based maps where the metro network is built. Each level has a specific layout of stations and resources, and players must adapt their strategies to the unique challenges of each level. The engine handles the logic for loading levels, spawning stations and passengers according to the level design, and updating the game state based on player actions within the context of the level. Each level may have different types of stations (shapes like circle, square, triangle, hexagon, star) and varying resource availability, which adds complexity and variety to the gameplay.

## Lines connecting stations

Players can create metro lines that connect two or more stations. Each line has a route that defines the order of stations it connects. The engine handles the logic for creating, removing, and extending lines based on the player's actions and updates the `GameState` accordingly.

Lines connect stations using a line segment that is always horizontal, vertical, or diagonal at a 45-degree angle. When a line is created or extended, the engine checks for valid connections between stations and updates the line's route accordingly. Horizontal, vertical, and diagonal connections can be combined andare determined based on the coordinates of the stations being connected. There should always be only one diagonal segment in a line and there can be horizontal or vertical segments added before, or after the diagonal segment, but not both.

Lines cross tiles on the grid and are always segments between the center of tiles, horizontally, vertically or diagonally.

## Movement of trains

Trains move along the metro lines according to the routes defined by the player. Each train moves one tile per hour and move through the whole line route visiting all the stations in one direction. Only when the train reaches the end of the line route, it will reverse direction and start moving back through the line visiting all the stations in the opposite direction. The engine handles the logic for moving trains along the lines, updating their positions in the `GameState`, and processing passenger boarding and alighting at stations.