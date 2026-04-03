using MetroMania.Demo;
using MetroMania.Demo.Levels;
using MetroMania.Engine;

// ── Resolve paths ────────────────────────────────────────────────────────────

var resourcesPath = FindResourcesPath()
    ?? throw new DirectoryNotFoundException("Could not find the 'resources' folder. Run from the repo root.");

var outputPath = Path.Combine(AppContext.BaseDirectory, "output", Guid.NewGuid().ToString());
Directory.CreateDirectory(outputPath);

// ── Run simulation ────────────────────────────────────────────────────────────

var level  = Level1.Level;
var runner = new MyMetroManiaRunner();
var engine = new MetroManiaEngine();

Console.WriteLine($"Running simulation for level: {level.Title}");
Console.WriteLine($"Max days: {level.LevelData.MaxDays}");

var result = engine.Run(runner, level, maxHours: level.LevelData.MaxDays * 24);

Console.WriteLine($"Simulation complete: {result.DaysSurvived} days survived, {result.GameSnapshots.Count} snapshots");

// ── Render and save snapshots ─────────────────────────────────────────────────

using var renderer = new MetroManiaRenderer(resourcesPath);

Console.WriteLine($"Rendering {result.GameSnapshots.Count} snapshots to: {outputPath}");

for (int i = 0; i < result.GameSnapshots.Count; i++)
{
    var snapshot = result.GameSnapshots[i];
    var svg      = renderer.RenderSnapshot(level, snapshot);
    var fileName = $"{i + 1:D5}.svg";
    var filePath = Path.Combine(outputPath, fileName);

    await File.WriteAllTextAsync(filePath, svg);

    if (i % 100 == 0 || i == result.GameSnapshots.Count - 1)
        Console.WriteLine($"  [{i + 1}/{result.GameSnapshots.Count}] Day {snapshot.Time.Day} Hour {snapshot.Time.Hour:D2} → {fileName}");
}

// ── Write viewer HTML ─────────────────────────────────────────────────────────

var templatePath = Path.Combine(AppContext.BaseDirectory, "viewer.html");
var viewerHtml = (await File.ReadAllTextAsync(templatePath))
    .Replace("%%TOTAL%%", result.GameSnapshots.Count.ToString());
await File.WriteAllTextAsync(Path.Combine(outputPath, "viewer.html"), viewerHtml);

Console.WriteLine($"Viewer saved → {Path.Combine(outputPath, "viewer.html")}");
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
