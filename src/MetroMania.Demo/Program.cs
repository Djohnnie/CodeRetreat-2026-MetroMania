using System.Text.Json;
using System.Text.Json.Serialization;
using MetroMania.Demo;
using MetroMania.Demo.Levels;
using MetroMania.Demo.Runners;
using MetroMania.Engine;
using MetroMania.Engine.Model;

// ── Resolve paths ────────────────────────────────────────────────────────────

var resourcesPath = FindResourcesPath()
    ?? throw new DirectoryNotFoundException("Could not find the 'resources' folder. Run from the repo root.");

var outputPath = Path.Combine(AppContext.BaseDirectory, "output", Guid.NewGuid().ToString());
Directory.CreateDirectory(outputPath);

// ── Run simulation ────────────────────────────────────────────────────────────

using var renderer = new MetroManiaRenderer(resourcesPath);
var templatePath = Path.Combine(AppContext.BaseDirectory, "viewer.html");

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    Converters = { new JsonStringEnumConverter(), new LocationJsonConverter() }
};

foreach (var level in new[] { /*Level1.Level, Level2.Level, */Level3.Level })
{
    var levelOutputPath = Path.Combine(outputPath, level.Title.Replace(" ", "-").ToLowerInvariant());
    Directory.CreateDirectory(levelOutputPath);

    var runner = new SimpleRunner();
    var engine = new MetroManiaEngine();

    Console.WriteLine($"Running simulation for level: {level.Title}");
    Console.WriteLine($"Max days: {level.LevelData.MaxDays}");

    var result = engine.Run(runner, level, maxHours: level.LevelData.MaxDays * 24);

    Console.WriteLine($"Simulation complete: {result.DaysSurvived} days survived, {result.GameSnapshots.Count} snapshots");

    // ── Render and save snapshots ─────────────────────────────────────────────

    Console.WriteLine($"Rendering {result.GameSnapshots.Count} snapshots to: {levelOutputPath}");

    for (int i = 0; i < result.GameSnapshots.Count; i++)
    {
        var snapshot = result.GameSnapshots[i];
        var svg      = renderer.RenderSnapshot(level, snapshot);
        var fileName = $"{i + 1:D5}.svg";
        var filePath = Path.Combine(levelOutputPath, fileName);

        await File.WriteAllTextAsync(filePath, svg);

        var jsonPath = Path.ChangeExtension(filePath, ".json");
        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(snapshot, jsonOptions));

        if (i % 100 == 0 || i == result.GameSnapshots.Count - 1)
            Console.WriteLine($"  [{i + 1}/{result.GameSnapshots.Count}] Day {snapshot.Time.Day} Hour {snapshot.Time.Hour:D2} → {fileName}");
    }

    // ── Write viewer HTML ─────────────────────────────────────────────────────

    var viewerHtml = (await File.ReadAllTextAsync(templatePath))
        .Replace("%%TOTAL%%", result.GameSnapshots.Count.ToString())
        .Replace("%%LEVEL_TITLE%%", level.Title);
    await File.WriteAllTextAsync(Path.Combine(levelOutputPath, "viewer.html"), viewerHtml);

    Console.WriteLine($"Viewer saved → {Path.Combine(levelOutputPath, "viewer.html")}");
}

Console.WriteLine("Done.");

// ── Helper ────────────────────────────────────────────────────────────────────

static string? FindResourcesPath()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null)
    {
        var candidate = Path.Combine(dir.FullName, "resources");
        if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "background.svg")))
            return candidate;
        dir = dir.Parent;
    }
    return null;
}
