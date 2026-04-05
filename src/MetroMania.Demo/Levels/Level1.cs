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
        GridWidth = 16,
        GridHeight = 9,
        LevelData = new LevelData
        {
            Seed = 42,
            VehicleCapacity = 8,
            MaxDays = 50,
            Stations =
            [
                new MetroStation { GridX = 3, GridY = 4, StationType = StationType.Circle,    SpawnDelayDays = 0, PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 1 }] },
                new MetroStation { GridX = 12, GridY = 4, StationType = StationType.Rectangle, SpawnDelayDays = 0, PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 1 }] },
            ],
            InitialResources = [ResourceType.Line, ResourceType.Train]
        }
    };
}
