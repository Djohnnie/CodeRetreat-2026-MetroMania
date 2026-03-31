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

    /// <summary>
    /// Renders an existing game snapshot as an SVG string without running the engine.
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
        var tileCache = new Dictionary<string, SKPicture>();

        // Map each line's GUID to a display color, using index order for consistency.
        var lineColorMap = snapshot.Lines
            .Select((line, i) => (line.LineId, Color: LineColors[i % LineColors.Length]))
            .ToDictionary(x => x.LineId, x => x.Color);

        // Reverse-lookup: station GUID → grid location, needed for positioning vehicles.
        var stationLocations = snapshot.Stations
            .ToDictionary(kvp => kvp.Value.Id, kvp => kvp.Key);

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
            }
        }

        // Pass 2: metro lines drawn beneath station tiles
        DrawLines(canvas, snapshot, stationLocations, lineColorMap);

        // Pass 3: station tiles on top of lines
        foreach (var (loc, station) in snapshot.Stations)
        {
            DrawTile(canvas, tileCache, GetStationTileName(station.Type),
                loc.X * TileSize, loc.Y * TileSize, colorMap);
        }

        // Pass 4: passengers waiting at stations, rendered above station icons
        DrawWaitingPassengers(canvas, snapshot);

        // Pass 5: trains and wagons with their onboard passengers
        DrawVehicles(canvas, snapshot, stationLocations, lineColorMap);

        // Pass 6: header overlay on top of the first tile row
        DrawHeader(canvas, width, level.Title, snapshot.Time, snapshot.TotalScore);
        canvas.Dispose();
        skStream.Dispose();

        foreach (var pic in tileCache.Values)
            pic.Dispose();

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

        // Semi-transparent dark band over the top tile row
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
            Typeface = SKTypeface.FromFamilyName("sans-serif", SKFontStyle.Normal),
        };

        // Baseline: vertically centred within the header band
        SKFontMetrics metrics;
        textPaint.GetFontMetrics(out metrics);
        // Ascent is negative in SkiaSharp (distance upward from baseline).
        // This places the baseline so the text is visually centred in the band.
        float textY = headerHeight / 2f - (metrics.Ascent + metrics.Descent) / 2f;

        // Left side: title + day/hour counter (e.g. "My Level  1/06:00")
        string dayHour = $"{time.Day}/{time.Hour:D2}:00";
        string leftText = $"{levelTitle}  {dayHour}";
        canvas.DrawText(leftText, padding, textY, textPaint);

        // Right side: score
        string rightText = $"score: {score} points";
        float rightTextWidth = textPaint.MeasureText(rightText);
        canvas.DrawText(rightText, totalWidth - padding - rightTextWidth, textY, textPaint);
    }

    // ─── Metro lines ──────────────────────────────────────────────────────────

    private static void DrawLines(
        SKCanvas canvas,
        GameSnapshot snapshot,
        Dictionary<Guid, Location> stationLocations,
        Dictionary<Guid, SKColor> lineColorMap)
    {
        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = LineStrokeWidth,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
        };

        foreach (var line in snapshot.Lines)
        {
            if (!lineColorMap.TryGetValue(line.LineId, out var color)) continue;
            paint.Color = color;

            for (int i = 0; i < line.StationIds.Count - 1; i++)
            {
                if (!stationLocations.TryGetValue(line.StationIds[i], out var locA)) continue;
                if (!stationLocations.TryGetValue(line.StationIds[i + 1], out var locB)) continue;

                var (ax, ay) = StationCenter(locA);
                var (bx, by) = StationCenter(locB);
                canvas.DrawLine(ax, ay, bx, by, paint);
            }
        }
    }

    // ─── Waiting passengers ───────────────────────────────────────────────────

    private static void DrawWaitingPassengers(SKCanvas canvas, GameSnapshot snapshot)
    {
        const int maxPerRow = 10;

        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = new SKColor(0x33, 0x33, 0x33),
            IsAntialias = true,
        };

        foreach (var (loc, station) in snapshot.Stations)
        {
            var passengers = station.Passengers;
            if (passengers.Count == 0) continue;

            var (cx, _) = StationCenter(loc);
            float stationTopY = loc.Y * TileSize;

            int row1Count = Math.Min(passengers.Count, maxPerRow);
            int row2Count = Math.Min(passengers.Count - row1Count, maxPerRow);

            // Row 1 sits just above the station tile; row 2 stacks above row 1.
            float row1TopY = stationTopY - WaitingPassengerSize - 2f;
            float row2TopY = row1TopY - (WaitingPassengerSize + PassengerGap);

            DrawPassengerRow(canvas, paint, passengers, 0, row1Count, cx, row1TopY, WaitingPassengerSize);
            if (row2Count > 0)
                DrawPassengerRow(canvas, paint, passengers, row1Count, row1Count + row2Count, cx, row2TopY, WaitingPassengerSize);
        }
    }

    private static void DrawPassengerRow(
        SKCanvas canvas, SKPaint paint,
        IList<Passenger> passengers, int start, int end,
        float centerX, float topY, float size)
    {
        int count = end - start;
        if (count <= 0) return;

        float totalWidth = count * size + (count - 1) * PassengerGap;
        float startX = centerX - totalWidth / 2f;

        for (int i = 0; i < count; i++)
        {
            float px = startX + i * (size + PassengerGap);
            DrawPassengerIcon(canvas, paint, passengers[start + i].DestinationType, px, topY, size);
        }
    }

    // ─── Vehicles ────────────────────────────────────────────────────────────

    private static void DrawVehicles(
        SKCanvas canvas,
        GameSnapshot snapshot,
        Dictionary<Guid, Location> stationLocations,
        Dictionary<Guid, SKColor> lineColorMap)
    {
        // IDs that belong to wagons; we draw wagons offset from their parent train.
        var wagonIdSet = new HashSet<Guid>(snapshot.Vehicles.SelectMany(v => v.WagonIds));

        foreach (var train in snapshot.Vehicles.Where(v => !wagonIdSet.Contains(v.VehicleId)))
        {
            if (!lineColorMap.TryGetValue(train.LineId, out var lineColor)) continue;
            if (!TryGetVehiclePosition(train, snapshot, stationLocations,
                    out float vx, out float vy, out float angle))
                continue;

            // Wagons are drawn first (behind the train) so the train renders on top.
            for (int i = 0; i < train.WagonIds.Count; i++)
            {
                float offset = (TrainLength + 3f) * (i + 1);
                float wx = vx - MathF.Cos(angle) * offset;
                float wy = vy - MathF.Sin(angle) * offset;

                var wagon = snapshot.Vehicles.FirstOrDefault(v => v.VehicleId == train.WagonIds[i]);
                DrawVehicleRect(canvas, wx, wy, angle, lineColor,
                    wagon?.Passengers ?? Array.Empty<Passenger>());
            }

            DrawVehicleRect(canvas, vx, vy, angle, lineColor, train.Passengers);
        }
    }

    /// <summary>
    /// Interpolates the pixel center and facing angle for a vehicle on its line segment.
    /// Returns false when the vehicle cannot be positioned (e.g. line has fewer than 2 stations).
    /// </summary>
    private static bool TryGetVehiclePosition(
        VehicleSnapshot vehicle,
        GameSnapshot snapshot,
        Dictionary<Guid, Location> stationLocations,
        out float vx, out float vy, out float angle)
    {
        vx = vy = angle = 0;

        var line = snapshot.Lines.FirstOrDefault(l => l.LineId == vehicle.LineId);
        if (line is null || line.StationIds.Count < 2) return false;

        int seg = vehicle.SegmentIndex;
        if (seg < 0 || seg >= line.StationIds.Count - 1) return false;

        if (!stationLocations.TryGetValue(line.StationIds[seg], out var locA)) return false;
        if (!stationLocations.TryGetValue(line.StationIds[seg + 1], out var locB)) return false;

        var (ax, ay) = StationCenter(locA);
        var (bx, by) = StationCenter(locB);

        vx = ax + (bx - ax) * vehicle.Progress;
        vy = ay + (by - ay) * vehicle.Progress;

        // Angle points in the direction of travel; flip 180° when moving backward.
        angle = MathF.Atan2(by - ay, bx - ax);
        if (vehicle.Direction == -1)
            angle += MathF.PI;

        return true;
    }

    /// <summary>
    /// Draws a single train/wagon rectangle, oriented along <paramref name="angle"/>,
    /// centered at (<paramref name="cx"/>, <paramref name="cy"/>).
    /// </summary>
    private static void DrawVehicleRect(
        SKCanvas canvas, float cx, float cy, float angle,
        SKColor lineColor, IReadOnlyList<Passenger> passengers)
    {
        canvas.Save();
        canvas.Translate(cx, cy);
        canvas.RotateDegrees(angle * (180f / MathF.PI));

        var rect = SKRect.Create(-TrainLength / 2f, -TrainHeight / 2f, TrainLength, TrainHeight);

        using (var bodyPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = lineColor, IsAntialias = true })
            canvas.DrawRoundRect(rect, TrainCornerRadius, TrainCornerRadius, bodyPaint);

        using (var borderPaint = new SKPaint
               {
                   Style = SKPaintStyle.Stroke,
                   Color = SKColors.White,
                   StrokeWidth = TrainBorderWidth,
                   IsAntialias = true,
               })
            canvas.DrawRoundRect(rect, TrainCornerRadius, TrainCornerRadius, borderPaint);

        if (passengers.Count > 0)
            DrawPassengersInVehicle(canvas, passengers);

        canvas.Restore();
    }

    /// <summary>
    /// Draws passenger icons inside the train rectangle, arranged in a grid
    /// centered on the vehicle's local origin (0, 0).
    /// </summary>
    private static void DrawPassengersInVehicle(SKCanvas canvas, IReadOnlyList<Passenger> passengers)
    {
        const float padding = 2.5f;

        float innerW = TrainLength - padding * 2;
        float innerH = TrainHeight - padding * 2;

        int cols = Math.Max(1, (int)((innerW + PassengerGap) / (TrainPassengerSize + PassengerGap)));
        int rows = Math.Max(1, (int)((innerH + PassengerGap) / (TrainPassengerSize + PassengerGap)));
        int maxVisible = Math.Min(passengers.Count, cols * rows);

        int actualCols = Math.Min(maxVisible, cols);
        int actualRows = (int)Math.Ceiling((float)maxVisible / actualCols);

        float gridW = actualCols * TrainPassengerSize + (actualCols - 1) * PassengerGap;
        float gridH = actualRows * TrainPassengerSize + (actualRows - 1) * PassengerGap;
        float startX = -gridW / 2f;
        float startY = -gridH / 2f;

        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = SKColors.White,
            IsAntialias = true,
        };

        for (int i = 0; i < maxVisible; i++)
        {
            int row = i / actualCols;
            int col = i % actualCols;
            float px = startX + col * (TrainPassengerSize + PassengerGap);
            float py = startY + row * (TrainPassengerSize + PassengerGap);
            DrawPassengerIcon(canvas, paint, passengers[i].DestinationType, px, py, TrainPassengerSize);
        }
    }

    // ─── Passenger icon shapes ────────────────────────────────────────────────

    /// <summary>
    /// Draws a miniature shape matching the passenger's destination station type.
    /// The shape is drawn at (<paramref name="x"/>, <paramref name="y"/>) with the given <paramref name="size"/>.
    /// </summary>
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
        StationType.Pentagon => "station-pentagon",
        StationType.Star => "station-star",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown station type")
    };
}
