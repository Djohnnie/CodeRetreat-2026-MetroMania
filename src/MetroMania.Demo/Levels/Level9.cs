using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;

namespace MetroMania.Demo.Levels;

/// <summary>
/// Level 9 — Insert Station Demo
///
/// Demonstrates ExtendLineInBetween: a station is inserted into the middle
/// of an existing line while a train is already running.
///
/// Grid layout (GridWidth=16, GridHeight=10):
///
///   A ● (2,5) ─────── Line 1 ─────── C ■ (14,5)
///
///                  B ▲ (8,5)
///              (inserted mid-line later)
///
///              D ★ (8,2)
///                  |
///                Line 2
///                  |
///              B ▲ (8,5)
///
/// Setup sequence:
///   1. CreateLine:            Line 1 connects Circle(A) → Rectangle(C)
///   2. AddVehicleToLine:      Train 1 on Line 1 at Circle(A)
///   3. (wait a few ticks for the train to start moving)
///   4. ExtendLineInBetween:   Insert Triangle(B) into Line 1 between A and C
///   5. CreateLine:            Line 2 connects Triangle(B) → Star(D)
///   6. AddVehicleToLine:      Train 2 on Line 2 at Triangle(B)
///
/// After the insert, Line 1 becomes A → B → C. The train that was already
/// moving on Line 1 automatically has its path index recalculated.
/// </summary>
internal static class Level9
{
    public static Level Level => new Level
    {
        Id = Guid.NewGuid(),
        Title = "Level 9 - Insert Station",
        Description = "Insert-station demo: A train is running on a two-stop line when a third station is inserted in between using ExtendLineInBetween. The train seamlessly adjusts to the new route.",
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
                    PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 12 }]
                },
                // B — Triangle: will be inserted mid-line later
                new MetroStation
                {
                    GridX = 8, GridY = 5,
                    StationType = StationType.Triangle,
                    SpawnDelayDays = 0,
                    PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 14 }]
                },
                // C — Rectangle: Line 1 right terminal
                new MetroStation
                {
                    GridX = 14, GridY = 5,
                    StationType = StationType.Rectangle,
                    SpawnDelayDays = 0,
                    PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 12 }]
                },
                // D — Star: Line 2 terminal, above B
                new MetroStation
                {
                    GridX = 8, GridY = 2,
                    StationType = StationType.Star,
                    SpawnDelayDays = 0,
                    PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 18 }]
                },
            ]
        }
    };
}
