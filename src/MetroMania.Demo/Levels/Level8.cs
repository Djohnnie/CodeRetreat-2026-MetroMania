using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;

namespace MetroMania.Demo.Levels;

/// <summary>
/// Level 8 — Pass-Through Demo
///
/// Demonstrates the split-line rendering when a metro line travels through
/// a tile occupied by a station it is NOT connected to.
///
/// Grid layout (GridWidth=16, GridHeight=10):
///
///                  D ★ (8,2)
///                  |
///                Line 2
///                  |
///   A ● (2,5) ╌╌╌ B ▲ (8,5) ╌╌╌ C ■ (14,5)
///               Line 1 (pass-through at B)
///
/// Line 1 connects Circle(A) to Rectangle(C) in a straight horizontal path.
/// The path crosses tile (8,5) where Triangle(B) sits — but B is NOT on Line 1.
/// The renderer draws Line 1 as two parallel 1 px rails (with a 1 px gap)
/// on tile (8,5) and its immediate neighbours, making it clear that Line 1
/// does not actually stop at B.
///
/// Line 2 connects Triangle(B) to Star(D) vertically, giving B its own service.
/// </summary>
internal static class Level8
{
    public static Level Level => new Level
    {
        Id = Guid.NewGuid(),
        Title = "Level 8 - Pass-Through",
        Description = "Pass-through demo: Line 1 travels through Triangle's tile without stopping. The split-line rendering (two 1 px rails with a 1 px gap) distinguishes pass-through from a real connection.",
        GridWidth = 16,
        GridHeight = 10,
        LevelData = new LevelData
        {
            Seed = 42,
            VehicleCapacity = 6,
            MaxDays = 14,
            InitialResources = [ResourceType.Line, ResourceType.Line, ResourceType.Train, ResourceType.Train],
            Stations =
            [
                // A — Circle: Line 1 left terminal
                new MetroStation
                {
                    GridX = 2, GridY = 5,
                    StationType = StationType.Circle,
                    SpawnDelayDays = 0,
                    PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 16 }]
                },
                // B — Triangle: NOT connected to Line 1; served only by Line 2
                new MetroStation
                {
                    GridX = 8, GridY = 5,
                    StationType = StationType.Triangle,
                    SpawnDelayDays = 0,
                    PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 18 }]
                },
                // C — Rectangle: Line 1 right terminal
                new MetroStation
                {
                    GridX = 14, GridY = 5,
                    StationType = StationType.Rectangle,
                    SpawnDelayDays = 0,
                    PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 16 }]
                },
                // D — Star: Line 2 terminal, above B
                new MetroStation
                {
                    GridX = 8, GridY = 2,
                    StationType = StationType.Star,
                    SpawnDelayDays = 0,
                    PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 20 }]
                },
            ]
        }
    };
}
