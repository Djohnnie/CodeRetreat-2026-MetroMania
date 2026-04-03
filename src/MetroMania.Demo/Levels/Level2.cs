using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;

namespace MetroMania.Demo.Levels;

internal static class Level2
{
    public static Level Level => new Level
    {
        Id = Guid.NewGuid(),
        Title = "Level 2",
        Description = "Same layout as Level 1, but the second station appears on day 2 and passengers start spawning after day 3 at different rates.",
        GridWidth = 16,
        GridHeight = 9,
        LevelData = new LevelData
        {
            Seed = 42,
            VehicleCapacity = 8,
            MaxDays = 50,
            Stations =
            [
                new MetroStation { GridX = 3,  GridY = 3, StationType = StationType.Circle,    SpawnDelayDays = 0, PassengerSpawnPhases = [new() { AfterDays = 3, FrequencyInHours = 3 }] },
                new MetroStation { GridX = 12, GridY = 5, StationType = StationType.Rectangle, SpawnDelayDays = 1, PassengerSpawnPhases = [new() { AfterDays = 3, FrequencyInHours = 5 }] },
                new MetroStation { GridX = 10, GridY = 7, StationType = StationType.Triangle, SpawnDelayDays = 2, PassengerSpawnPhases = [new() { AfterDays = 3, FrequencyInHours = 7 }] },
            ]
        }
    };
}
