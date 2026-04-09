using System.Text.Json;
using System.Text.Json.Serialization;
using MetroMania.Demo;
using MetroMania.Demo.Levels;
using MetroMania.Demo.Runners;
using MetroMania.Domain.Entities;
using MetroMania.Engine;
using MetroMania.Engine.Contracts;
using MetroMania.Engine.Model;

// ── Resolve paths ────────────────────────────────────────────────────────────

var resourcesPath = FindResourcesPath()
    ?? throw new DirectoryNotFoundException("Could not find the 'resources' folder. Run from the repo root.");

var outputPath = Path.Combine(AppContext.BaseDirectory, "output", Guid.NewGuid().ToString());
Directory.CreateDirectory(outputPath);

// ── Run simulation ────────────────────────────────────────────────────────────

using var renderer = new MetroManiaRenderer(resourcesPath);

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    Converters = { new JsonStringEnumConverter(), new LocationJsonConverter() }
};

var levelRunnerPairs = new (Level Level, IMetroManiaRunner Runner, string? Tag)[]
{
    // (Level1.Level, new SimpleRunner(), null),
    // (Level2.Level, new SimpleRunner(), null),
    // (Level3.Level, new SimpleRunner(), null),
    // (Level4.Level, new TransferDemoRunner(), null),
    // (Level5.Level, new ShortcutTransferRunner(), null),
    // (Level6.Level, new RemoveVehicleDemoRunner(), null),
    // (Level7.Level, new RemoveLineDemoRunner(), null),
    // (Level8.Level, new PassThroughDemoRunner(), null),
    // (Level9.Level, new InsertStationDemoRunner(), null),
    // (Level10.Level, new MiddleInsertUpDemoRunner(), null),
    // (BonusLevel.Level, new OptimalRunner(), "optimal-baseline"),
    (BonusLevel.Level, new BonusLevelRunner(), null),
    (BonusLevel.Level, new UltraRunner(), "ultra"),

    // // Optimal runner — same levels, best general-purpose strategy
    // (Level1.Level, new OptimalRunner(), "optimal"),
    // (Level2.Level, new OptimalRunner(), "optimal"),
    // (Level3.Level, new OptimalRunner(), "optimal"),
    // (Level4.Level, new OptimalRunner(), "optimal"),
    // (Level5.Level, new OptimalRunner(), "optimal"),
    // (Level6.Level, new OptimalRunner(), "optimal"),
    // (Level7.Level, new OptimalRunner(), "optimal"),
};

foreach (var (level, runner, tag) in levelRunnerPairs)
{
    var folderName = level.Title.Replace(" ", "-").ToLowerInvariant();
    if (tag is not null) folderName += $"-{tag}";
    var levelOutputPath = Path.Combine(outputPath, folderName);
    Directory.CreateDirectory(levelOutputPath);

    var engine = new MetroManiaEngine();

    var displayTitle = tag is not null ? $"{level.Title} ({tag})" : level.Title;

    Console.WriteLine($"Running simulation for level: {displayTitle}");
    Console.WriteLine($"Max days: {level.LevelData.MaxDays}");

    var result = engine.Run(runner, level, maxHours: level.LevelData.MaxDays * 24);

    Console.WriteLine($"Simulation complete: {result.DaysSurvived} days survived, score: {result.TotalScore}, {result.GameSnapshots.Count} snapshots");

    // ── Render and save snapshots ─────────────────────────────────────────────

    Console.WriteLine($"Rendering {result.GameSnapshots.Count} snapshots to: {levelOutputPath}");

    for (int i = 0; i < result.GameSnapshots.Count; i++)
    {
        var snapshot = result.GameSnapshots[i];
        var svg = renderer.RenderSnapshot(level, snapshot);
        var fileName = $"{i + 1:D5}.svg";
        var filePath = Path.Combine(levelOutputPath, fileName);

        await File.WriteAllTextAsync(filePath, svg);

        var jsonPath = Path.ChangeExtension(filePath, ".json");
        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(snapshot, jsonOptions));

        if (i % 100 == 0 || i == result.GameSnapshots.Count - 1)
            Console.WriteLine($"  [{i + 1}/{result.GameSnapshots.Count}] Day {snapshot.Time.Day} Hour {snapshot.Time.Hour:D2} → {fileName}");
    }

    // ── Write viewer HTML ─────────────────────────────────────────────────────

    var viewerHtml = ViewerTemplate.Generate(displayTitle, result.GameSnapshots.Count, padWidth: 5);
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