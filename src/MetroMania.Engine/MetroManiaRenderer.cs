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

                DrawTile(canvas, tileCache, "background", px, py, colorMap);

                if (waterSet.Contains((x, y)))
                {
                    // Treat out-of-bounds neighbors as water so edges blend seamlessly
                    bool IsWaterOrEdge(int nx, int ny) =>
                        nx < 0 || ny < 0 || nx >= level.GridWidth || ny >= level.GridHeight
                        || waterSet.Contains((nx, ny));

                    bool n = IsWaterOrEdge(x, y - 1);
                    bool e = IsWaterOrEdge(x + 1, y);
                    bool s = IsWaterOrEdge(x, y + 1);
                    bool w = IsWaterOrEdge(x - 1, y);
                    bool ne = n && e && IsWaterOrEdge(x + 1, y - 1);
                    bool se = e && s && IsWaterOrEdge(x + 1, y + 1);
                    bool sw = s && w && IsWaterOrEdge(x - 1, y + 1);
                    bool nw = w && n && IsWaterOrEdge(x - 1, y - 1);

                    string tileName = GetWaterTileName(n, ne, e, se, s, sw, w, nw);
                    DrawTile(canvas, tileCache, tileName, px, py, colorMap);
                }

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

        // If the exact tile doesn't exist, fall back to the cardinal-only version
        // (with all relevant diagonals treated as water)
        if (!File.Exists(path))
        {
            var fallback = GetFallbackTileName(tileName);
            if (fallback is not null)
                path = Path.Combine(_svgResourcesPath, $"{fallback}.svg");
        }

        if (!File.Exists(path))
            throw new FileNotFoundException($"SVG tile not found: {path}");

        var svgText = File.ReadAllText(path);

        foreach (var (sourceColor, targetColor) in colorMap)
            svgText = svgText.Replace(sourceColor, targetColor);

        var svg = new SKSvg();
        svg.FromSvg(svgText);
        return svg.Picture ?? throw new InvalidOperationException($"Failed to load SVG picture from: {path}");
    }

    /// <summary>
    /// Falls back to the "all relevant diagonals water" version of the same cardinal combination.
    /// </summary>
    private static string? GetFallbackTileName(string tileName)
    {
        // Parse which directions are present in the tile name
        if (!tileName.StartsWith("water"))
            return null;

        var suffix = tileName == "water" ? "" : tileName["water-".Length..];
        var parts = suffix.Length > 0 ? suffix.Split('-') : [];
        var dirs = new HashSet<string>(parts);

        bool n = dirs.Contains("N"), e = dirs.Contains("E"),
             s = dirs.Contains("S"), w = dirs.Contains("W");

        // Rebuild with all relevant diagonals present
        return BuildWaterTileName(n, n && e, e, e && s, s, s && w, w, w && n);
    }

    private static string GetWaterTileName(bool n, bool ne, bool e, bool se, bool s, bool sw, bool w, bool nw)
        => BuildWaterTileName(n, ne, e, se, s, sw, w, nw);

    private static string BuildWaterTileName(bool n, bool ne, bool e, bool se, bool s, bool sw, bool w, bool nw)
    {
        var parts = new List<string>(8);
        if (n) parts.Add("N");
        if (n && e && ne) parts.Add("NE");
        if (e) parts.Add("E");
        if (e && s && se) parts.Add("SE");
        if (s) parts.Add("S");
        if (s && w && sw) parts.Add("SW");
        if (w) parts.Add("W");
        if (w && n && nw) parts.Add("NW");
        return parts.Count == 0 ? "water" : "water-" + string.Join("-", parts);
    }

    private static string GetStationTileName(StationType type) => type switch
    {
        StationType.Circle => "station-circle",
        StationType.Rectangle => "station-square",
        StationType.Triangle => "station-triangle",
        StationType.Diamond => "station-diamond",
        StationType.Cross => "station-polygon",
        StationType.Ruby => "station-star",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown station type")
    };
}
