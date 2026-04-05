using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;

namespace MetroMania.Demo.Levels;

/// <summary>
/// Level 5 — Shortcut Transfer Demo
///
/// A U-shaped arc (Line 1) with a diagonal shortcut (Line 2) that cuts directly
/// from the first bend to the far end of the arc.  Passengers wanting to reach
/// Star(E) must transfer at Rectangle(B) instead of riding the full arc.
///
/// Grid layout (GridWidth=16, GridHeight=10):
///
///   B(2,2) ——— C(7,2) ——— D(12,2)
///   |  \                      |
///   |   \  Line 2 (shortcut)  |
///   |    \                    |
///   A(2,7)                 E(12,7)
///
///   Line 1 (arc):      A → B → C → D → E   (5+5+5+5 = 20 tile-steps)
///   Line 2 (shortcut): B → E               (Chebyshev max(10,5) = 10 tile-steps)
///
/// Rectangle (B) is the interchange: both lines stop there.
///
/// Key transfer scenario — passenger at Circle(A) wanting Star(E):
///   Via Line 1 alone:       A→B→C→D→E = 20 steps
///   Via transfer at B:      A→B (5) + B→E on Line 2 (10) = 15 steps  ← optimal
///
/// The engine's transfer-drop logic drops the passenger at B because continuing
/// on Line 1 toward C costs 5+10=15 steps total, which is NOT equal to the
/// global optimum of 10 steps from B.  The passenger then waits at B and boards
/// the Line 2 train for direct delivery to E.
/// </summary>
internal static class Level5
{
    public static Level Level => new Level
    {
        Id = Guid.NewGuid(),
        Title = "Level 5",
        Description = "Shortcut transfer: passengers riding the U-shaped arc transfer at the Rectangle interchange to the direct Line 2 shortcut instead of following the full arc to Star.",
        GridWidth = 16,
        GridHeight = 10,
        LevelData = new LevelData
        {
            Seed = 77,
            VehicleCapacity = 6,
            MaxDays = 30,
            InitialResources = [ResourceType.Line, ResourceType.Line, ResourceType.Train, ResourceType.Train],
            Stations =
            [
                // A — Circle: arc start, bottom-left
                new MetroStation
                {
                    GridX = 2, GridY = 7,
                    StationType = StationType.Circle,
                    SpawnDelayDays = 0,
                    PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 15 }]
                },
                // B — Rectangle: interchange (on both lines), top-left
                new MetroStation
                {
                    GridX = 2, GridY = 2,
                    StationType = StationType.Rectangle,
                    SpawnDelayDays = 0,
                    PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 20 }]
                },
                // C — Triangle: Line 1 mid-stop, top-middle
                new MetroStation
                {
                    GridX = 7, GridY = 2,
                    StationType = StationType.Triangle,
                    SpawnDelayDays = 0,
                    PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 22 }]
                },
                // D — Diamond: Line 1 mid-stop, top-right
                new MetroStation
                {
                    GridX = 12, GridY = 2,
                    StationType = StationType.Diamond,
                    SpawnDelayDays = 0,
                    PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 22 }]
                },
                // E — Star: arc end + Line 2 terminus, bottom-right
                new MetroStation
                {
                    GridX = 12, GridY = 7,
                    StationType = StationType.Star,
                    SpawnDelayDays = 0,
                    PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 15 }]
                },
            ]
        }
    };
}
