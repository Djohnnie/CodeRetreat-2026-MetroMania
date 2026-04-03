using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;
using MetroMania.Engine.Model;
using SkiaSharp;
using Svg.Skia;

namespace MetroMania.Engine;

/// <summary>
/// Renders a MetroMania game snapshot as an SVG string.
/// Uses SkiaSharp with Svg.Skia to load tile SVGs and compose them onto an SVG canvas.
/// </summary>
public class MetroManiaRenderer(string svgResourcesPath) : IDisposable
{
    private const int TileSize = 32;
    private const float LineStrokeWidth = 5f;
    private const float TrainLength = 22f;
    private const float TrainHeight = 12f;
    private const float TrainCornerRadius = 2f;
    private const float TrainBorderWidth = 1.5f;
    private const float WaitingPassengerSize = 5f;
    private const float TrainPassengerSize = 3f;
    private const float PassengerGap = 1.5f;

    private static readonly SKColor[] LineColors =
    [
        new SKColor(0xE5, 0x39, 0x35), // Red
        new SKColor(0x19, 0x76, 0xD2), // Blue
        new SKColor(0x38, 0x8E, 0x3C), // Green
        new SKColor(0xF5, 0x7F, 0x17), // Orange
        new SKColor(0x71, 0x20, 0x9B), // Purple
        new SKColor(0x00, 0x97, 0xA7), // Teal
        new SKColor(0xAD, 0x14, 0x57), // Pink
        new SKColor(0x37, 0x47, 0x4F), // Slate
    ];

    /// <summary>The background color baked into the SVG tile assets.</summary>
    private const string SourceBackgroundColor = "rgb(255,227,214)";

    /// <summary>The water color baked into the SVG tile assets.</summary>
    private const string SourceWaterColor = "rgb(182,227,243)";

    private readonly string _svgResourcesPath = svgResourcesPath;

    /// <summary>
    /// Tile picture cache shared across all RenderSnapshot() calls on this renderer instance.
    /// Keyed by tile name + color map fingerprint so different level color themes coexist.
    /// </summary>
    private readonly Dictionary<string, SKPicture> _tileCache = new();

    /// <summary>
    /// Renders an existing game snapshot as an SVG string.
    /// </summary>
    public string RenderSnapshot(Level level, GameSnapshot snapshot)
        => Compose(level, snapshot);

    private string Compose(Level level, GameSnapshot snapshot)
    {
        int width = level.GridWidth * TileSize;
        int height = level.GridHeight * TileSize;

        var waterSet = new HashSet<(int X, int Y)>(
            level.LevelData.WaterTiles.Select(w => (w.GridX, w.GridY)));

        var colorMap = BuildColorMap(level.LevelData);

        using var stream = new MemoryStream();
        using var skStream = new SKManagedWStream(stream);
        using var canvas = SKSvgCanvas.Create(SKRect.Create(width, height), skStream);

        // Pass 1: background and water tiles
        for (int y = 0; y < level.GridHeight; y++)
        {
            for (int x = 0; x < level.GridWidth; x++)
            {
                float px = x * TileSize;
                float py = y * TileSize;

                DrawTile(canvas, "background", px, py, colorMap);

                if (waterSet.Contains((x, y)))
                {
                    bool IsWaterOrEdge(int nx, int ny) =>
                        nx < 0 || ny < 0 || nx >= level.GridWidth || ny >= level.GridHeight
                        || waterSet.Contains((nx, ny));

                    bool n  = IsWaterOrEdge(x, y - 1);
                    bool e  = IsWaterOrEdge(x + 1, y);
                    bool s  = IsWaterOrEdge(x, y + 1);
                    bool w  = IsWaterOrEdge(x - 1, y);
                    bool ne = n && e && IsWaterOrEdge(x + 1, y - 1);
                    bool se = e && s && IsWaterOrEdge(x + 1, y + 1);
                    bool sw = s && w && IsWaterOrEdge(x - 1, y + 1);
                    bool nw = w && n && IsWaterOrEdge(x - 1, y - 1);

                    string tileName = GetWaterTileName(n, ne, e, se, s, sw, w, nw);
                    DrawTile(canvas, tileName, px, py, colorMap);
                }
            }
        }

        // Pass 2: station tiles
        foreach (var (loc, station) in snapshot.Stations)
        {
            DrawTile(canvas, GetStationTileName(station.StationType),
                loc.X * TileSize, loc.Y * TileSize, colorMap);
        }

        // Pass 3: header overlay on top of the first tile row
        DrawHeader(canvas, width, level.Title, snapshot.Time, snapshot.Score);

        // Pass 4: player action overlay in the bottom-right (only when an action was taken)
        if (snapshot.LastAction is not null and not NoAction)
            DrawPlayerAction(canvas, width, level.GridHeight, snapshot.LastAction);

        canvas.Dispose();
        skStream.Dispose();

        stream.Position = 0;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    // ─── Header overlay ───────────────────────────────────────────────────────

    private static void DrawHeader(SKCanvas canvas, int totalWidth, string levelTitle, GameTime time, int score)
    {
        const float headerHeight = TileSize;
        const float fontSize = 13f;
        const float padding = 8f;

        using var bgPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = new SKColor(0, 0, 0, 168),
        };
        canvas.DrawRect(0, 0, totalWidth, headerHeight, bgPaint);

        using var textPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = SKColors.White,
            IsAntialias = true,
            TextSize = fontSize,
            Typeface = SKTypeface.FromFamilyName("Liberation Sans", SKFontStyle.Normal)
                    ?? SKTypeface.FromFamilyName("sans-serif", SKFontStyle.Normal)
                    ?? SKTypeface.Default,
        };

        textPaint.GetFontMetrics(out SKFontMetrics metrics);
        float textY = headerHeight / 2f - (metrics.Ascent + metrics.Descent) / 2f;

        string dayHour = $"{time.Day}/{time.Hour:D2}:00";
        string leftText = $"{levelTitle}  {dayHour}";
        using var leftPath = textPaint.GetTextPath(leftText, padding, textY);
        canvas.DrawPath(leftPath, textPaint);

        string rightText = $"score: {score}";
        float rightTextWidth = textPaint.MeasureText(rightText);
        using var rightPath = textPaint.GetTextPath(rightText, totalWidth - padding - rightTextWidth, textY);
        canvas.DrawPath(rightPath, textPaint);
    }

    // ─── Player action overlay ────────────────────────────────────────────────

    private static void DrawPlayerAction(SKCanvas canvas, int totalWidth, int gridHeight, PlayerAction action)
    {
        string? text = DescribeAction(action);
        if (text is null) return;

        const float fontSize = 13f;
        const float padding  = 8f;

        using var textPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = SKColors.White,
            IsAntialias = true,
            TextSize = fontSize,
            Typeface = SKTypeface.FromFamilyName("Liberation Sans", SKFontStyle.Normal)
                    ?? SKTypeface.FromFamilyName("sans-serif", SKFontStyle.Normal)
                    ?? SKTypeface.Default,
        };

        float textWidth = textPaint.MeasureText(text);
        float bgWidth   = textWidth + padding * 2f;
        float tileY     = (gridHeight - 1) * TileSize;

        using var bgPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = new SKColor(0, 0, 0, 168) };
        canvas.DrawRect(totalWidth - bgWidth, tileY, bgWidth, TileSize, bgPaint);

        textPaint.GetFontMetrics(out var metrics);
        float textY = tileY + TileSize / 2f - (metrics.Ascent + metrics.Descent) / 2f;
        float textX = totalWidth - padding - textWidth;

        using var textPath = textPaint.GetTextPath(text, textX, textY);
        canvas.DrawPath(textPath, textPaint);
    }

    private static string? DescribeAction(PlayerAction action) => action switch
    {
        NoAction          => null,
        CreateLine        => "Line created",
        RemoveLine        => "Line removed",
        AddVehicleToLine  => "Train deployed",
        RemoveVehicle     => "Train removed",
        _                 => null,
    };

    // ─── Passenger icon shapes ────────────────────────────────────────────────

    private static void DrawPassengerIcon(
        SKCanvas canvas, SKPaint paint,
        StationType type, float x, float y, float size)
    {
        float r = size / 2f;
        float cx = x + r;
        float cy = y + r;

        switch (type)
        {
            case StationType.Circle:
                canvas.DrawCircle(cx, cy, r, paint);
                break;

            case StationType.Rectangle:
                canvas.DrawRect(x, y, size, size, paint);
                break;

            case StationType.Triangle:
            {
                using var path = new SKPath();
                path.MoveTo(cx, y);
                path.LineTo(x + size, y + size);
                path.LineTo(x, y + size);
                path.Close();
                canvas.DrawPath(path, paint);
                break;
            }

            case StationType.Diamond:
            {
                using var path = new SKPath();
                path.MoveTo(cx, y);
                path.LineTo(x + size, cy);
                path.LineTo(cx, y + size);
                path.LineTo(x, cy);
                path.Close();
                canvas.DrawPath(path, paint);
                break;
            }

            case StationType.Pentagon:
            {
                using var path = MakeRegularPolygonPath(cx, cy, r, 5, -MathF.PI / 2f);
                canvas.DrawPath(path, paint);
                break;
            }

            case StationType.Star:
            {
                using var path = MakeStarPath(cx, cy, r, r * 0.45f, 5);
                canvas.DrawPath(path, paint);
                break;
            }
        }
    }

    private static SKPath MakeRegularPolygonPath(float cx, float cy, float r, int sides, float startAngle)
    {
        var path = new SKPath();
        for (int i = 0; i < sides; i++)
        {
            float angle = startAngle + i * (2f * MathF.PI / sides);
            float px = cx + r * MathF.Cos(angle);
            float py = cy + r * MathF.Sin(angle);
            if (i == 0) path.MoveTo(px, py);
            else path.LineTo(px, py);
        }
        path.Close();
        return path;
    }

    private static SKPath MakeStarPath(float cx, float cy, float outerR, float innerR, int points)
    {
        var path = new SKPath();
        float startAngle = -MathF.PI / 2f;
        for (int i = 0; i < points * 2; i++)
        {
            float angle = startAngle + i * (MathF.PI / points);
            float r = i % 2 == 0 ? outerR : innerR;
            float px = cx + r * MathF.Cos(angle);
            float py = cy + r * MathF.Sin(angle);
            if (i == 0) path.MoveTo(px, py);
            else path.LineTo(px, py);
        }
        path.Close();
        return path;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static (float x, float y) StationCenter(Location loc)
        => (loc.X * TileSize + TileSize / 2f, loc.Y * TileSize + TileSize / 2f);

    private static (float px, float py) TileToPixel(float tx, float ty)
        => (tx * TileSize + TileSize / 2f, ty * TileSize + TileSize / 2f);

    /// <summary>
    /// Computes the tile-coordinate waypoints for a metro line segment between two stations.
    /// Lines are drawn as straight horizontal or vertical segments; when a direct H/V connection
    /// is not possible, a single 45-degree diagonal is inserted between the parts.
    /// Total path length equals the Chebyshev distance: max(|dx|, |dy|) tiles.
    /// </summary>
    private static List<(float x, float y)> ComputeMetroPath(Location a, Location b)
    {
        int dx = b.X - a.X;
        int dy = b.Y - a.Y;
        int absDx = Math.Abs(dx);
        int absDy = Math.Abs(dy);
        int sdx = Math.Sign(dx);
        int sdy = Math.Sign(dy);
        int diagLen = Math.Min(absDx, absDy);
        int startStraight = Math.Abs(absDx - absDy) / 2;

        float x0 = a.X, y0 = a.Y;
        float x3 = b.X, y3 = b.Y;
        float x1, y1, x2, y2;

        if (absDx >= absDy) // horizontal-dominant: straight portions are horizontal
        {
            x1 = x0 + sdx * startStraight; y1 = y0;
            x2 = x1 + sdx * diagLen;       y2 = y1 + sdy * diagLen;
        }
        else // vertical-dominant: straight portions are vertical
        {
            x1 = x0;                 y1 = y0 + sdy * startStraight;
            x2 = x1 + sdx * diagLen; y2 = y1 + sdy * diagLen;
        }

        var pts = new List<(float, float)> { (x0, y0) };
        if (x1 != x0 || y1 != y0) pts.Add((x1, y1));
        if (x2 != x1 || y2 != y1) pts.Add((x2, y2));
        if (x3 != pts[^1].Item1 || y3 != pts[^1].Item2) pts.Add((x3, y3));

        return pts;
    }

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
        string tileName,
        float x, float y,
        Dictionary<string, string> colorMap)
    {
        var cacheKey = colorMap.Count == 0 ? tileName
            : tileName + "|" + string.Join(",", colorMap.Select(kv => $"{kv.Key}={kv.Value}"));

        if (!_tileCache.TryGetValue(cacheKey, out var picture))
        {
            picture = LoadSvgPicture(tileName, colorMap);
            _tileCache[cacheKey] = picture;
        }

        canvas.Save();
        canvas.Translate(x, y);
        canvas.DrawPicture(picture);
        canvas.Restore();
    }

    public void Dispose()
    {
        foreach (var pic in _tileCache.Values)
            pic.Dispose();
        _tileCache.Clear();
    }

    private SKPicture LoadSvgPicture(string tileName, Dictionary<string, string> colorMap)
    {
        var path = Path.Combine(_svgResourcesPath, $"{tileName}.svg");

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
        if (!tileName.StartsWith("water"))
            return null;

        var suffix = tileName == "water" ? "" : tileName["water-".Length..];
        var parts = suffix.Length > 0 ? suffix.Split('-') : [];
        var dirs = new HashSet<string>(parts);

        bool n = dirs.Contains("N"), e = dirs.Contains("E"),
             s = dirs.Contains("S"), w = dirs.Contains("W");

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
        StationType.Circle    => "station-circle",
        StationType.Rectangle => "station-square",
        StationType.Triangle  => "station-triangle",
        StationType.Diamond   => "station-diamond",
        StationType.Pentagon  => "station-pentagon",
        StationType.Star      => "station-star",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown station type")
    };
}