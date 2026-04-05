using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;

namespace MetroMania.Demo.Levels;

/// <summary>
/// Level 4 — Transfer Demo
///
/// Layout (grid columns 3 / 8 / 13, rows 2 and 5):
///
///                       Triangle (8,2)
///                            |
///                          Line 2
///                            |
///   Circle (3,5) ——— Rectangle (8,5) ——— Star (13,5)
///                         Line 1
///
/// Rectangle is the interchange: passengers wanting Triangle board
/// Line 1 toward Rectangle, are dropped there by the transfer logic,
/// then picked up by the Line 2 train for delivery to Triangle.
///
/// Two extra Lines and two extra Trains are provided upfront via
/// InitialResources so TransferDemoRunner can set everything up
/// within the first few ticks.
/// </summary>
internal static class Level4
{
    public static Level Level => new Level
    {
        Id = Guid.NewGuid(),
        Title = "Level 4",
        Description = "Transfer demo: passengers use Line 1 to reach the Rectangle interchange, then switch to Line 2 to reach their Triangle destination.",
        GridWidth = 16,
        GridHeight = 9,
        LevelData = new LevelData
        {
            Seed = 99,
            VehicleCapacity = 6,
            MaxDays = 30,
            InitialResources = [ResourceType.Line, ResourceType.Line, ResourceType.Train, ResourceType.Train],
            Stations =
            [
                // Line 1 left terminal — spawns immediately, moderate passenger rate
                new MetroStation
                {
                    GridX = 3, GridY = 5,
                    StationType = StationType.Circle,
                    SpawnDelayDays = 0,
                    PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 18 }]
                },
                // Interchange — sits at the junction of both lines
                new MetroStation
                {
                    GridX = 8, GridY = 5,
                    StationType = StationType.Rectangle,
                    SpawnDelayDays = 0,
                    PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 20 }]
                },
                // Line 1 right terminal — not reachable from Line 2
                new MetroStation
                {
                    GridX = 13, GridY = 5,
                    StationType = StationType.Star,
                    SpawnDelayDays = 0,
                    PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 22 }]
                },
                // Line 2 terminal — only reachable via the Rectangle interchange
                new MetroStation
                {
                    GridX = 8, GridY = 2,
                    StationType = StationType.Triangle,
                    SpawnDelayDays = 0,
                    PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 22 }]
                },
            ]
        }
    };
}
