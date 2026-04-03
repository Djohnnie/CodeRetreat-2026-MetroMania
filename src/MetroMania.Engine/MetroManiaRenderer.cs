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

        // Map each line's GUID to a display color, using index order for consistency.
        var lineColorMap = snapshot.Lines
            .Select((line, i) => (line.LineId, Color: LineColors[i % LineColors.Length]))
            .ToDictionary(x => x.LineId, x => x.Color);

        // Reverse-lookup: station GUID → grid location, needed to position lines.
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

        // Pass 2: metro lines drawn beneath station tiles
        DrawLines(canvas, snapshot, stationLocations, lineColorMap);

        // Pass 3: station tiles on top of lines
        foreach (var (loc, station) in snapshot.Stations)
        {
            DrawTile(canvas, GetStationTileName(station.StationType),
                loc.X * TileSize, loc.Y * TileSize, colorMap);
        }

        // Pass 4: passengers waiting at stations, rendered above station icons
        DrawWaitingPassengers(canvas, snapshot);

        // Pass 5: trains on top of lines and stations
        DrawVehicles(canvas, snapshot, stationLocations, lineColorMap);

        // Pass 6: header overlay on top of the first tile row
        DrawHeader(canvas, width, level.Title, snapshot.Time, snapshot.Score);

        // Pass 7: resource availability counts in the bottom-left column
        DrawResourceCounts(canvas, level.GridHeight, snapshot);

        // Pass 8: player action overlay in the bottom-right (only when an action was taken)
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
                if (!stationLocations.TryGetValue(line.StationIds[i],     out var locA)) continue;
                if (!stationLocations.TryGetValue(line.StationIds[i + 1], out var locB)) continue;

                var waypoints = LinePathHelper.ComputeSegmentWaypoints(locA, locB);
                for (int j = 0; j < waypoints.Count - 1; j++)
                {
                    var (ax, ay) = TileToPixel(waypoints[j].X,     waypoints[j].Y);
                    var (bx, by) = TileToPixel(waypoints[j + 1].X, waypoints[j + 1].Y);
                    canvas.DrawLine(ax, ay, bx, by, paint);
                }
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
            var passengers = snapshot.Passengers
                .Where(p => p.StationId == station.Id)
                .ToList();
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

    // ─── Vehicles ─────────────────────────────────────────────────────────────

    private static void DrawVehicles(
        SKCanvas canvas,
        GameSnapshot snapshot,
        Dictionary<Guid, Location> stationLocations,
        Dictionary<Guid, SKColor> lineColorMap)
    {
        foreach (var train in snapshot.Trains)
        {
            if (!lineColorMap.TryGetValue(train.LineId, out var lineColor)) continue;

            var line = snapshot.Lines.FirstOrDefault(l => l.LineId == train.LineId);
            if (line is null) continue;

            var tilePath = LinePathHelper.ComputeTilePath(line, stationLocations);
            if (tilePath.Count == 0) continue;

            var (vx, vy) = TileToPixel(train.TilePosition.X, train.TilePosition.Y);

            // Facing angle: look at the next tile in the direction of travel.
            float angle = 0f;
            int currentIndex = tilePath.IndexOf(train.TilePosition);
            if (currentIndex != -1 && tilePath.Count > 1)
            {
                int nextIndex = currentIndex + train.Direction;
                // At a terminal the train will flip next tick; show it facing back the way it came.
                if (nextIndex < 0 || nextIndex >= tilePath.Count)
                    nextIndex = currentIndex - train.Direction;
                nextIndex = Math.Clamp(nextIndex, 0, tilePath.Count - 1);

                var nextTile = tilePath[nextIndex];
                angle = MathF.Atan2(
                    nextTile.Y - train.TilePosition.Y,
                    nextTile.X - train.TilePosition.X);
            }

            DrawVehicleRect(canvas, vx, vy, angle, lineColor, []);
        }
    }

    /// <summary>
    /// Draws a single train rectangle, oriented along <paramref name="angle"/> and
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

        using var bodyPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = lineColor,
            IsAntialias = true,
        };
        canvas.DrawRoundRect(rect, TrainCornerRadius, TrainCornerRadius, bodyPaint);

        using var borderPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.White,
            StrokeWidth = TrainBorderWidth,
            IsAntialias = true,
        };
        canvas.DrawRoundRect(rect, TrainCornerRadius, TrainCornerRadius, borderPaint);

        if (passengers.Count > 0)
            DrawPassengersInVehicle(canvas, passengers);

        canvas.Restore();
    }

    /// <summary>
    /// Draws passenger icons inside the vehicle, arranged in a grid centred on the vehicle origin.
    /// </summary>
    private static void DrawPassengersInVehicle(SKCanvas canvas, IReadOnlyList<Passenger> passengers)
    {
        const float padding = 2.0f;

        float innerW = TrainLength - padding * 2;
        float innerH = TrainHeight - padding * 2;

        int cols = Math.Max(1, (int)((innerW + PassengerGap) / (TrainPassengerSize + PassengerGap)));
        int rows = Math.Max(1, (int)((innerH + PassengerGap) / (TrainPassengerSize + PassengerGap)));
        int maxVisible = Math.Min(passengers.Count, cols * rows);

        int actualCols = Math.Min(maxVisible, cols);
        int actualRows = (int)MathF.Ceiling((float)maxVisible / actualCols);

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

    // ─── Resource counts HUD ──────────────────────────────────────────────────

    /// <summary>
    /// Draws the available resource counts in the bottom three tiles of the first column.
    /// Each tile shows a resource icon (line / train / wagon) and the available count.
    /// </summary>
    private static void DrawResourceCounts(SKCanvas canvas, int gridHeight, GameSnapshot snapshot)
    {
        int availableLines  = snapshot.Resources.Count(r => !r.InUse && r.Type == ResourceType.Line);
        int availableTrains = snapshot.Resources.Count(r => !r.InUse && r.Type == ResourceType.Train);
        int availableWagons = snapshot.Resources.Count(r => !r.InUse && r.Type == ResourceType.Wagon);

        // Bottom 3 tiles of column 0, from top to bottom: line, train, wagon
        (int RowOffset, ResourceType Type, int Count)[] items =
        [
            (-3, ResourceType.Line,  availableLines),
            (-2, ResourceType.Train, availableTrains),
            (-1, ResourceType.Wagon, availableWagons),
        ];

        using var bgPaint    = new SKPaint { Style = SKPaintStyle.Fill, Color = new SKColor(0, 0, 0, 168) };
        using var iconFill   = new SKPaint { Style = SKPaintStyle.Fill, Color = SKColors.White, IsAntialias = true };
        using var iconStroke = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.White,
            StrokeWidth = 4f,
            StrokeCap = SKStrokeCap.Round,
            IsAntialias = true,
        };
        using var textPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = SKColors.White,
            IsAntialias = true,
            TextSize = 11f,
            Typeface = SKTypeface.FromFamilyName("Liberation Sans", SKFontStyle.Normal)
                    ?? SKTypeface.FromFamilyName("sans-serif", SKFontStyle.Normal)
                    ?? SKTypeface.Default,
        };

        textPaint.GetFontMetrics(out var metrics);

        foreach (var (rowOffset, type, count) in items)
        {
            int row = gridHeight + rowOffset;
            if (row < 0) continue;

            float tileY       = row * TileSize;
            float tileCenterY = tileY + TileSize / 2f;

            canvas.DrawRect(0, tileY, TileSize, TileSize, bgPaint);

            DrawResourceIcon(canvas, type, 10f, tileCenterY, iconFill, iconStroke);

            string countStr = count.ToString();
            float textY = tileCenterY - (metrics.Ascent + metrics.Descent) / 2f;
            using var textPath = textPaint.GetTextPath(countStr, 20f, textY);
            canvas.DrawPath(textPath, textPaint);
        }
    }

    /// <summary>
    /// Draws a small icon representing the given resource type, centered at (<paramref name="cx"/>, <paramref name="cy"/>).
    /// Uses the Material Design SVG path for each resource type, scaled to 16×16.
    /// </summary>
    private static void DrawResourceIcon(
        SKCanvas canvas, ResourceType type, float cx, float cy,
        SKPaint fillPaint, SKPaint strokePaint)
    {
        string? pathData = type switch
        {
            ResourceType.Line  => "M19.5,9.5c-1.03,0-1.9,0.62-2.29,1.5h-2.92C13.9,10.12,13.03,9.5,12,9.5s-1.9,0.62-2.29,1.5H6.79" +
                                  " C6.4,10.12,5.53,9.5,4.5,9.5C3.12,9.5,2,10.62,2,12s1.12,2.5,2.5,2.5c1.03,0,1.9-0.62,2.29-1.5h2.92" +
                                  "c0.39,0.88,1.26,1.5,2.29,1.5s1.9-0.62,2.29-1.5h2.92c0.39,0.88,1.26,1.5,2.29,1.5c1.38,0,2.5-1.12,2.5-2.5" +
                                  "S20.88,9.5,19.5,9.5z",
            ResourceType.Train => "M12 2c-4 0-8 .5-8 4v9.5C4 17.43 5.57 19 7.5 19L6 20.5v.5h2.23l2-2H14l2 2h2v-.5L16.5 19" +
                                  "c1.93 0 3.5-1.57 3.5-3.5V6c0-3.5-3.58-4-8-4zM7.5 17c-.83 0-1.5-.67-1.5-1.5S6.67 14 7.5 14" +
                                  "s1.5.67 1.5 1.5S8.33 17 7.5 17zm3.5-7H6V6h5v4zm2 0V6h5v4h-5zm3.5 7c-.83 0-1.5-.67-1.5-1.5" +
                                  "s.67-1.5 1.5-1.5 1.5.67 1.5 1.5-.67 1.5-1.5 1.5z",
            ResourceType.Wagon => "M17 5H3c-1.1 0-2 .89-2 2v9h2c0 1.65 1.34 3 3 3s3-1.35 3-3h5.5c0 1.65 1.34 3 3 3s3-1.35 3-3H23" +
                                  "v-5l-6-6zM3 11V7h4v4H3zm3 6.5c-.83 0-1.5-.67-1.5-1.5s.67-1.5 1.5-1.5 1.5.67 1.5 1.5-.67 1.5-1.5 1.5z" +
                                  "m7-6.5H9V7h4v4zm4.5 6.5c-.83 0-1.5-.67-1.5-1.5s.67-1.5 1.5-1.5 1.5.67 1.5 1.5-.67 1.5-1.5 1.5z" +
                                  "M15 11V7h1l4 4h-5z",
            _ => null
        };

        if (pathData is null) return;

        const float iconSize = 16f;
        const float scale = iconSize / 24f;

        using var iconPath = SKPath.ParseSvgPathData(pathData);
        canvas.Save();
        canvas.Translate(cx - iconSize / 2f, cy - iconSize / 2f);
        canvas.Scale(scale);
        canvas.DrawPath(iconPath, fillPaint);
        canvas.Restore();
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
