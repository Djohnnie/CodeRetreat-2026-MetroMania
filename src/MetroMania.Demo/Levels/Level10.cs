using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;

namespace MetroMania.Demo.Levels;

/// <summary>
/// Level 10 — Mid-Line Upward Insert Demo
///
/// Two stations on a horizontal line with a third station spawning above the
/// midpoint. When the runner inserts the third station into the middle of the
/// line, the route visually bends upward and the train must adjust.
///
/// Grid layout (GridWidth=16, GridHeight=10):
///
///             B ▲ (8,2)          ← spawns here, inserted mid-line later
///            ╱         ╲
///   A ● (2,5)           C ■ (14,5)
///
/// Setup sequence:
///   1. CreateLine A → C (horizontal)
///   2. AddVehicleToLine: deploy train at A
///   3. Wait a few ticks so the train is mid-segment
///   4. ExtendLineInBetween: insert B between A and C → line bends up
///
/// The interesting part: the train was travelling the straight A→C path and is
/// now on a path that detours through B three rows higher. The engine snaps the
/// train to the nearest tile on the new path.
/// </summary>
internal static class Level10
{
    public static Level Level => new Level
    {
        Id = Guid.NewGuid(),
        Title = "Level 10 - Mid Insert Up",
        Description = "Two stations on a flat line, a third station spawns above the center. "
                    + "Inserting it mid-line causes the route to bend upward — watch the train adjust.",
        GridWidth = 16,
        GridHeight = 10,
        LevelData = new LevelData
        {
            Seed = 42,
            VehicleCapacity = 6,
            MaxDays = 14,
            InitialResources = [ResourceType.Line, ResourceType.Train],
            Stations =
            [
                // A — Circle: left terminal
                new MetroStation
                {
                    GridX = 2, GridY = 5,
                    StationType = StationType.Circle,
                    SpawnDelayDays = 0,
                    PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 12 }]
                },
                // B — Triangle: above center, inserted mid-line later
                new MetroStation
                {
                    GridX = 8, GridY = 2,
                    StationType = StationType.Triangle,
                    SpawnDelayDays = 0,
                    PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 14 }]
                },
                // C — Rectangle: right terminal
                new MetroStation
                {
                    GridX = 14, GridY = 5,
                    StationType = StationType.Rectangle,
                    SpawnDelayDays = 0,
                    PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 12 }]
                },
            ]
        }
    };
}
