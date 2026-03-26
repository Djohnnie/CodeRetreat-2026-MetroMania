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
        Seed = 42,
        Stations =
        [
            new MetroStation { GridX = 2, GridY = 3, StationType = StationType.Circle, SpawnDelayDays = 1, PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 8 }, new() { AfterDays = 5, FrequencyInHours = 4 }] },
            new MetroStation { GridX = 5, GridY = 1, StationType = StationType.Rectangle, SpawnDelayDays = 1, PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 8 }, new() { AfterDays = 5, FrequencyInHours = 4 }] },
            new MetroStation { GridX = 8, GridY = 5, StationType = StationType.Triangle, SpawnDelayDays = 3, PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 6 }, new() { AfterDays = 4, FrequencyInHours = 3 }] },
            new MetroStation { GridX = 1, GridY = 7, StationType = StationType.Diamond, SpawnDelayDays = 5, PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 6 }] },
        ],
        WaterTiles =
        [
            new Water { GridX = 4, GridY = 4 },
            new Water { GridX = 5, GridY = 4 },
            new Water { GridX = 6, GridY = 4 },
            new Water { GridX = 4, GridY = 5 },
            new Water { GridX = 5, GridY = 5 },
            new Water { GridX = 6, GridY = 5 },
            new Water { GridX = 5, GridY = 6 },
        ]
    }
};

// --- Run the full game ---
var engine = new MetroManiaEngine();
var runner = new MyMetroManiaRunner();
var result = engine.Run(runner, level);

Console.WriteLine($"Game Over!");
Console.WriteLine($"  Score:              {result.Score} (hours survived)");
Console.WriteLine($"  Days Survived:      {result.DaysSurvived}");
Console.WriteLine($"  Passengers Spawned: {result.TotalPassengersSpawned}");
Console.WriteLine($"  Time Taken:         {result.TimeTaken.TotalMilliseconds:F0}ms");

// --- Render the level at different points in time ---
var svgResourcesPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "resources"));
if (!Directory.Exists(svgResourcesPath))
{
    Console.WriteLine($"\nSVG resources not found at: {svgResourcesPath}");
    Console.WriteLine("Trying current directory parent...");
    svgResourcesPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "resources"));
}

Console.WriteLine($"\nSVG resources path: {svgResourcesPath}");

var renderer = new MetroManiaRenderer(engine, svgResourcesPath);
var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "output");
Directory.CreateDirectory(outputDir);

int[] hoursToRender = [0, 24, 72, 120];
foreach (var hours in hoursToRender)
{
    var outputPath = Path.Combine(outputDir, $"level-at-{hours}h.svg");
    renderer.RenderToSvg(new MyMetroManiaRunner(), level, hours, outputPath);
    Console.WriteLine($"  Rendered level at {hours}h -> {outputPath}");
}

Console.WriteLine($"\nAll renders saved to: {outputDir}");