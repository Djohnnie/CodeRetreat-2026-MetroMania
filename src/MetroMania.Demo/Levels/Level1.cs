using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;

namespace MetroMania.Demo.Levels;

internal static class Level1
{
    public static Level Level => new Level
    {
        Id = Guid.NewGuid(),
        Title = "Level 1",
        Description = "A simple level with two stations.",
        GridWidth = 4,
        GridHeight = 1,
        LevelData = new LevelData
        {
            Seed = 42,
            VehicleCapacity = 6,
            MaxDays = 200,
            Stations =
            [
                new MetroStation { GridX = 0, GridY = 0, StationType = StationType.Circle,    SpawnDelayDays = 0, PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 1 }] },
                new MetroStation { GridX = 3, GridY = 0, StationType = StationType.Rectangle, SpawnDelayDays = 0, PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 1 }] },
            ]
        }
    };
}
