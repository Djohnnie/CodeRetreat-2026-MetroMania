using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;
using MetroMania.Engine.Contracts;
using MetroMania.Engine.Model;
using Moq;

namespace MetroMania.Engine.Tests;

public class MetroManiaEngineTests
{
    [Fact]
    public void Run_SingleStation_CallsOnStationSpawnedWithCorrectLocationAndType()
    {
        var runner = new Mock<IMetroManiaRunner>();
        var level = new Level
        {
            Title = "Test Level",
            GridWidth = 1,
            GridHeight = 1,
            LevelData = new LevelData
            {
                Seed = 123,
                Stations =
                [
                    new MetroStation
                    {
                        GridX = 0,
                        GridY = 0,
                        StationType = StationType.Triangle,
                        SpawnDelayDays = 0,
                        PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 8 }]
                    }
                ]
            }
        };

        var engine = new MetroManiaEngine();
        engine.Run(runner.Object, level);

        runner.Verify(
            r => r.OnStationSpawned(new Location(0, 0), StationType.Triangle),
            Times.Once);
    }
}