using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;

namespace MetroMania.Demo.Levels;

/// <summary>
/// A short level designed to demonstrate RemoveVehicle with passengers on board.
/// Two stations 9 tiles apart, passengers spawn every hour at both stations.
/// The runner creates a line, deploys a train, lets it pick up passengers,
/// then removes the train to observe the pending-removal / force-drop behavior.
/// </summary>
internal static class Level6
{
    public static Level Level => new()
    {
        Id = Guid.NewGuid(),
        Title = "Level 6 - RemoveVehicle Demo",
        Description = "Demonstrates removing a train that has passengers on board.",
        GridWidth = 16,
        GridHeight = 9,
        LevelData = new LevelData
        {
            Seed = 42,
            VehicleCapacity = 6,
            MaxDays = 5,
            Stations =
            [
                new MetroStation { GridX = 3, GridY = 4, StationType = StationType.Circle,    SpawnDelayDays = 0, PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 4 }] },
                new MetroStation { GridX = 12, GridY = 4, StationType = StationType.Rectangle, SpawnDelayDays = 0, PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 4 }] },
            ],
            InitialResources = [ResourceType.Line, ResourceType.Train]
        }
    };
}
