using MetroMania.Demo.Levels;
using MetroMania.Demo.Runners;
using MetroMania.Engine;

var level = Level1.Level;


// --- Run the full game ---
var engine = new MetroManiaEngine();
var runner = new SimpleRunner();
var result = engine.Run(runner, level);

Console.WriteLine($"Game Over!");
Console.WriteLine($"  Score:              {result.Score}");
Console.WriteLine($"  Days Survived:      {result.DaysSurvived}");
Console.WriteLine($"  Passengers Spawned: {result.TotalPassengersSpawned}");
Console.WriteLine($"  Time Taken:         {result.TimeTaken.TotalMilliseconds:F0}ms");


//// --- Render the level at different points in time ---
//var svgResourcesPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "resources"));
//if (!Directory.Exists(svgResourcesPath))
//{
//    Console.WriteLine($"\nSVG resources not found at: {svgResourcesPath}");
//    Console.WriteLine("Trying current directory parent...");
//    svgResourcesPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "resources"));
//}

//Console.WriteLine($"\nSVG resources path: {svgResourcesPath}");

//var renderer = new MetroManiaRenderer(engine, svgResourcesPath);
//var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "output");
//Directory.CreateDirectory(outputDir);

//int[] hoursToRender = [0, 24, 72, 120];
//foreach (var hours in hoursToRender)
//{
//    var outputPath = Path.Combine(outputDir, $"level-at-{hours}h.svg");
//    renderer.RenderToSvg(new MyMetroManiaRunner(), level, hours, outputPath);
//    Console.WriteLine($"  Rendered level at {hours}h -> {outputPath}");
//}

//Console.WriteLine($"\nAll renders saved to: {outputDir}");