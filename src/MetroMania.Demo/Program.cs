using MetroMania.Demo;
using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;
using MetroMania.Engine;

var level = new Level
{
    Title = "Demo Level",
    Description = "A simple demo level with four stations",
    GridWidth = 10,
    GridHeight = 8,
    LevelData = new LevelData
    {
        Stations =
        [
            new MetroStation { GridX = 2, GridY = 3, StationType = StationType.Circle, SpawnOrder = 1, SpawnDelayDays = 1, SpawnSeed = 100 },
            new MetroStation { GridX = 5, GridY = 1, StationType = StationType.Rectangle, SpawnOrder = 2, SpawnDelayDays = 1, SpawnSeed = 200 },
            new MetroStation { GridX = 8, GridY = 5, StationType = StationType.Triangle, SpawnOrder = 3, SpawnDelayDays = 3, SpawnSeed = 300 },
            new MetroStation { GridX = 1, GridY = 7, StationType = StationType.Diamond, SpawnOrder = 4, SpawnDelayDays = 5, SpawnSeed = 400 },
        ],
        WaterTiles =
        [
            new Water { GridX = 4, GridY = 4 },
            new Water { GridX = 5, GridY = 4 },
            new Water { GridX = 6, GridY = 4 },
        ]
    }
};

var engine = new MetroManiaEngine();
var runner = new MyMetroManiaRunner();
var result = engine.Run(runner, level);

Console.WriteLine($"Game Over!");
Console.WriteLine($"  Score:              {result.Score} (hours survived)");
Console.WriteLine($"  Days Survived:      {result.DaysSurvived}");
Console.WriteLine($"  Passengers Spawned: {result.TotalPassengersSpawned}");
Console.WriteLine($"  Time Taken:         {result.TimeTaken.TotalMilliseconds:F0}ms");