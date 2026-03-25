using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;
using MetroMania.Engine.Contracts;
using MetroMania.Engine.Model;
using SkiaSharp;
using Svg.Skia;

namespace MetroMania.Engine;

/// <summary>
/// Renders a MetroMania level at a specific point in time as an SVG file.
/// Uses SkiaSharp with Svg.Skia to load tile SVGs and compose them onto an SVG canvas.
/// </summary>
public class MetroManiaRenderer
{
    private const int TileSize = 32;

    /// <summary>The background color baked into the SVG tile assets.</summary>
    private const string SourceBackgroundColor = "rgb(255,227,214)";

    /// <summary>The water color baked into the SVG tile assets.</summary>
    private const string SourceWaterColor = "rgb(182,227,243)";

    private readonly MetroManiaEngine _engine;
    private readonly string _svgResourcesPath;

    public MetroManiaRenderer(MetroManiaEngine engine, string svgResourcesPath)
    {
        _engine = engine;
        _svgResourcesPath = svgResourcesPath;
    }

    /// <summary>
    /// Runs the engine for the given number of hours, then renders the level state as an SVG file.
    /// </summary>
    public void RenderToSvg(IMetroManiaRunner runner, Level level, int totalHours, string outputPath)
    {
        var snapshot = _engine.RunForHours(runner, level, totalHours);
        var svg = Compose(level, snapshot);
        File.WriteAllText(outputPath, svg);
    }

    /// <summary>
    /// Runs the engine for the given number of hours, then returns the composed SVG as a string.
    /// </summary>
    public string RenderToSvgString(IMetroManiaRunner runner, Level level, int totalHours)
    {
        var snapshot = _engine.RunForHours(runner, level, totalHours);
        return Compose(level, snapshot);
    }

    private string Compose(Level level, GameSnapshot snapshot)
    {
        int width = level.GridWidth * TileSize;
        int height = level.GridHeight * TileSize;

        var waterSet = new HashSet<(int X, int Y)>(
            level.LevelData.WaterTiles.Select(w => (w.GridX, w.GridY)));

        var colorMap = BuildColorMap(level.LevelData);
        var tileCache = new Dictionary<string, SKPicture>();

        using var stream = new MemoryStream();
        using var skStream = new SKManagedWStream(stream);
        using var canvas = SKSvgCanvas.Create(SKRect.Create(width, height), skStream);

        for (int y = 0; y < level.GridHeight; y++)
        {
            for (int x = 0; x < level.GridWidth; x++)
            {
                float px = x * TileSize;
                float py = y * TileSize;

                // Always draw background first so it shows through transparent areas
                DrawTile(canvas, tileCache, "01-background", px, py, colorMap);

                if (waterSet.Contains((x, y)))
                {
                    bool n = waterSet.Contains((x, y - 1));
                    bool e = waterSet.Contains((x + 1, y));
                    bool s = waterSet.Contains((x, y + 1));
                    bool w = waterSet.Contains((x - 1, y));

                    string tileName = GetWaterTileName(n, e, s, w);
                    DrawTile(canvas, tileCache, tileName, px, py, colorMap);

                    // Inner corner overlays for any tile with adjacent water neighbors
                    if (n && e && !waterSet.Contains((x + 1, y - 1))) // NE diagonal is land
                        DrawTile(canvas, tileCache, "37-water-SW", px, py, colorMap);
                    if (e && s && !waterSet.Contains((x + 1, y + 1))) // SE diagonal is land
                        DrawTile(canvas, tileCache, "38-water-WN", px, py, colorMap);
                    if (s && w && !waterSet.Contains((x - 1, y + 1))) // SW diagonal is land
                        DrawTile(canvas, tileCache, "35-water-NE", px, py, colorMap);
                    if (w && n && !waterSet.Contains((x - 1, y - 1))) // NW diagonal is land
                        DrawTile(canvas, tileCache, "36-water-ES", px, py, colorMap);
                }

                // Station overlay
                if (snapshot.Stations.TryGetValue(new Location(x, y), out var station))
                {
                    string stationTile = GetStationTileName(station.Type);
                    DrawTile(canvas, tileCache, stationTile, px, py, colorMap);
                }
            }
        }

        canvas.Dispose();
        skStream.Dispose();

        foreach (var pic in tileCache.Values)
            pic.Dispose();

        stream.Position = 0;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Builds a color replacement map from the level data.
    /// Maps the hardcoded SVG tile colors to the level's custom colors.
    /// </summary>
    private static Dictionary<string, string> BuildColorMap(LevelData data)
    {
        var map = new Dictionary<string, string>();

        if (!string.IsNullOrWhiteSpace(data.BackgroundColor))
            map[SourceBackgroundColor] = data.BackgroundColor;

        if (!string.IsNullOrWhiteSpace(data.WaterColor))
            map[SourceWaterColor] = data.WaterColor;

        return map;
    }

    private void DrawTile(
        SKCanvas canvas,
        Dictionary<string, SKPicture> cache,
        string tileName,
        float x, float y,
        Dictionary<string, string> colorMap)
    {
        if (!cache.TryGetValue(tileName, out var picture))
        {
            picture = LoadSvgPicture(tileName, colorMap);
            cache[tileName] = picture;
        }

        canvas.Save();
        canvas.Translate(x, y);
        canvas.DrawPicture(picture);
        canvas.Restore();
    }

    private SKPicture LoadSvgPicture(string tileName, Dictionary<string, string> colorMap)
    {
        var path = Path.Combine(_svgResourcesPath, $"{tileName}.svg");
        if (!File.Exists(path))
            throw new FileNotFoundException($"SVG tile not found: {path}");

        var svgText = File.ReadAllText(path);

        // Replace hardcoded tile colors with the level's custom colors
        foreach (var (sourceColor, targetColor) in colorMap)
            svgText = svgText.Replace(sourceColor, targetColor);

        var svg = new SKSvg();
        svg.FromSvg(svgText);
        return svg.Picture ?? throw new InvalidOperationException($"Failed to load SVG picture from: {path}");
    }

    private static string GetWaterTileName(bool n, bool e, bool s, bool w) => (n, e, s, w) switch
    {
        (true, true, true, true) => "10-water-full",
        (false, false, false, false) => "11-water-no-neighbours",
        (false, true, false, true) => "12-water-WE",
        (true, false, true, false) => "13-water-NS",
        (true, true, false, true) => "14-water-WNE",
        (true, true, true, false) => "15-water-NES",
        (false, true, true, true) => "16-water-ESW",
        (true, false, true, true) => "17-water-SWN",
        (true, false, false, false) => "18-water-N",
        (false, true, false, false) => "19-water-E",
        (false, false, true, false) => "20-water-S",
        (false, false, false, true) => "21-water-W",
        (true, true, false, false) => "31-water-NE",
        (false, true, true, false) => "32-water-ES",
        (false, false, true, true) => "33-water-SW",
        (true, false, false, true) => "34-water-WN"
    };

    private static string GetStationTileName(StationType type) => type switch
    {
        StationType.Circle => "91-station-circle",
        StationType.Rectangle => "92-station-square",
        StationType.Triangle => "93-station-triangle",
        StationType.Diamond => "94-station-diamond",
        StationType.Cross => "95-station-polygon",
        StationType.Ruby => "96-station-star",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown station type")
    };
}
