using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;

namespace MetroMania.Demo.Levels;

/// <summary>
/// Level 7 — RemoveLine Demo
///
/// Layout (a long horizontal line with 4 different station types):
///
///   Circle (2,4) ——— Rectangle (6,4) ——— Triangle (10,4) ——— Diamond (14,4)
///
/// Three trains shuttle back and forth. Passengers spawn every 3 hours at each
/// station, giving the trains enough time to fill up. After ~30 ticks the runner
/// removes the entire line, triggering pending removal on all 3 trains.
///
/// The demo shows:
///   - All 3 trains entering pending removal simultaneously
///   - Each train dropping passengers one by one at whatever station it reaches
///   - Force-drops at non-matching stations (no score)
///   - Normal deliveries at matching stations (score)
///   - OnVehicleRemoved firing for each train as it empties
///   - OnLineRemoved firing after the last train is gone
/// </summary>
internal static class Level7
{
    public static Level Level => new()
    {
        Id = Guid.NewGuid(),
        Title = "Level 7 - RemoveLine Demo",
        Description = "Demonstrates removing a line with 3 active trains carrying passengers.",
        GridWidth = 18,
        GridHeight = 9,
        LevelData = new LevelData
        {
            Seed = 42,
            VehicleCapacity = 6,
            MaxDays = 10,
            InitialResources =
            [
                ResourceType.Line,
                ResourceType.Train, ResourceType.Train, ResourceType.Train
            ],
            Stations =
            [
                new MetroStation { GridX = 2,  GridY = 4, StationType = StationType.Circle,    SpawnDelayDays = 0, PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 3 }] },
                new MetroStation { GridX = 6,  GridY = 4, StationType = StationType.Rectangle, SpawnDelayDays = 0, PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 3 }] },
                new MetroStation { GridX = 10, GridY = 4, StationType = StationType.Triangle,  SpawnDelayDays = 0, PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 3 }] },
                new MetroStation { GridX = 14, GridY = 4, StationType = StationType.Diamond,   SpawnDelayDays = 0, PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 3 }] },
            ]
        }
    };
}
