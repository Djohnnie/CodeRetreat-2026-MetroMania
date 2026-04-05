using System.Globalization;
using System.Text;
using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;
using MetroMania.Engine.Model;
using static System.FormattableString;

namespace MetroMania.Engine;

/// <summary>
/// Renders a MetroMania game snapshot as a compact SVG string.
/// Builds SVG directly with a <see cref="StringBuilder"/> — no SkiaSharp SVG canvas —
/// so that HUD text is emitted as SVG <c>&lt;text&gt;</c> elements (not bezier-curve path
/// outlines), the background is a single <c>&lt;rect&gt;</c>, and repeated tile shapes are
/// defined once in <c>&lt;defs&gt;</c> and referenced with <c>&lt;use&gt;</c>.
///
/// <para>
/// <strong>Coordinate system:</strong> grid uses integer (column, row) tile coordinates
/// where (0, 0) is the top-left corner.  All pixel positions are derived by multiplying
/// tile coordinates by <see cref="TileSize"/> (32 px/tile) and adding <see cref="TileSize"/>/2
/// to reach the pixel centre of the tile.  X increases rightward, Y increases downward
/// (standard SVG convention).
/// </para>
/// </summary>
public class MetroManiaRenderer(string svgResourcesPath) : IDisposable
{
    // ── Tile/grid constants ───────────────────────────────────────────────────
    /// <summary>Pixels per grid tile. Changing this scales the entire output uniformly.</summary>
    private const int TileSize = 32;

    // ── Line drawing constants ────────────────────────────────────────────────
    /// <summary>Stroke width of metro lines in pixels.</summary>
    private const float LineStrokeWidth = 5f;

    // ── Train shape constants (all in pixels, drawn in local space centred at origin) ──
    /// <summary>Full length of the train body along its travel axis, in pixels.</summary>
    private const float TrainLength = 22f;
    /// <summary>Height (perpendicular to travel) of the train body, in pixels.</summary>
    private const float TrainHeight = 12f;
    /// <summary>Pixel radius of the two rounded rear corners of the train rectangle.</summary>
    private const float TrainCornerRadius = 2f;
    /// <summary>Stroke width of the white outline drawn over the filled train body, in pixels.</summary>
    private const float TrainBorderWidth = 1.5f;

    // ── Passenger icon constants (all in pixels) ──────────────────────────────
    /// <summary>Side length of each destination icon drawn above a station on the platform, in pixels.</summary>
    private const float WaitingPassengerSize = 5f;
    /// <summary>Side length of each destination icon drawn inside a train body, in pixels.</summary>
    private const float TrainPassengerSize = 3f;
    /// <summary>Gap between adjacent passenger icons (both on platforms and inside trains), in pixels.</summary>
    private const float PassengerGap = 1.5f;

    private static readonly string[] LineColors =
    [
        "#E53935", // Red
        "#1976D2", // Blue
        "#388E3C", // Green
        "#F57F17", // Orange
        "#71209B", // Purple
        "#0097A7", // Teal
        "#AD1457", // Pink
        "#37474F", // Slate
    ];

    /// <summary>The background color baked into the SVG tile assets.</summary>
    private const string SourceBackgroundColor = "rgb(255,227,214)";

    /// <summary>The water color baked into the SVG tile assets.</summary>
    private const string SourceWaterColor = "rgb(182,227,243)";

    // ── SVG path data for constant geometry ───────────────────────────────────

    /// <summary>
    /// Train body path in local space centred at origin, derived from the constants above.
    /// xl=-11, xr=11, yt=-6, yb=6, r=2, notch=3.
    /// </summary>
    private const string TrainBodySvgPath =
        "M-9,-6 L8,-6 L11,0 L8,6 L-9,6 A2,2 0 0 1 -11,4 L-11,-4 A2,2 0 0 1 -9,-6 Z";

    // Resource icon SVG path data (Material Design icons, 24×24 viewBox).
    private const string LineIconSvgPath =
        "M19.5,9.5c-1.03,0-1.9,0.62-2.29,1.5h-2.92C13.9,10.12,13.03,9.5,12,9.5s-1.9,0.62-2.29,1.5H6.79" +
        " C6.4,10.12,5.53,9.5,4.5,9.5C3.12,9.5,2,10.62,2,12s1.12,2.5,2.5,2.5c1.03,0,1.9-0.62,2.29-1.5h2.92" +
        "c0.39,0.88,1.26,1.5,2.29,1.5s1.9-0.62,2.29-1.5h2.92c0.39,0.88,1.26,1.5,2.29,1.5c1.38,0,2.5-1.12,2.5-2.5" +
        "S20.88,9.5,19.5,9.5z";
    private const string TrainIconSvgPath =
        "M12 2c-4 0-8 .5-8 4v9.5C4 17.43 5.57 19 7.5 19L6 20.5v.5h2.23l2-2H14l2 2h2v-.5L16.5 19" +
        "c1.93 0 3.5-1.57 3.5-3.5V6c0-3.5-3.58-4-8-4zM7.5 17c-.83 0-1.5-.67-1.5-1.5S6.67 14 7.5 14" +
        "s1.5.67 1.5 1.5S8.33 17 7.5 17zm3.5-7H6V6h5v4zm2 0V6h5v4h-5zm3.5 7c-.83 0-1.5-.67-1.5-1.5" +
        "s.67-1.5 1.5-1.5 1.5.67 1.5 1.5-.67 1.5-1.5 1.5z";
    private const string WagonIconSvgPath =
        "M17 5H3c-1.1 0-2 .89-2 2v9h2c0 1.65 1.34 3 3 3s3-1.35 3-3h5.5c0 1.65 1.34 3 3 3s3-1.35 3-3H23" +
        "v-5l-6-6zM3 11V7h4v4H3zm3 6.5c-.83 0-1.5-.67-1.5-1.5s.67-1.5 1.5-1.5 1.5.67 1.5 1.5-.67 1.5-1.5 1.5z" +
        "m7-6.5H9V7h4v4zm4.5 6.5c-.83 0-1.5-.67-1.5-1.5s.67-1.5 1.5-1.5 1.5.67 1.5 1.5-.67 1.5-1.5 1.5z" +
        "M15 11V7h1l4 4h-5z";

    // ── HUD constants ─────────────────────────────────────────────────────────
    private const string HudFont = "Liberation Sans,sans-serif";
    private const string HudBgFill = "rgba(0,0,0,0.659)";

    private readonly string _svgResourcesPath = svgResourcesPath;

    /// <summary>
    /// Tile inner-SVG cache shared across all RenderSnapshot() calls on this renderer instance.
    /// Keyed by tile name + colour map fingerprint. Each entry is the extracted inner markup
    /// of the tile SVG file (after colour substitutions), ready to embed inside a &lt;g&gt; or &lt;defs&gt;.
    /// </summary>
    private readonly Dictionary<string, string> _tileInnerSvgCache = new();

    /// <summary>
    /// Renders an existing game snapshot as an SVG string.
    /// Delegates to <see cref="Compose"/> which orchestrates all drawing passes.
    /// </summary>
    public string RenderSnapshot(Level level, GameSnapshot snapshot)
        => Compose(level, snapshot);

    /// <summary>
    /// Orchestrates all drawing passes for a single snapshot, composing the final
    /// SVG image in strict painter's-algorithm order so that later passes appear
    /// on top of earlier ones.
    ///
    /// <para><strong>Drawing pass order:</strong></para>
    /// <list type="number">
    ///   <item>Background rect — terrain foundation.</item>
    ///   <item>Water tiles via <c>&lt;use&gt;</c> references to <c>&lt;defs&gt;</c>.</item>
    ///   <item>Metro lines — drawn below stations so track doesn't overdraw station icons at terminals.</item>
    ///   <item>Station tiles via <c>&lt;use&gt;</c> references.</item>
    ///   <item>Waiting passengers — crowd indicators above station icons.</item>
    ///   <item>Trains — always the topmost game element so trains are never occluded.</item>
    ///   <item>Header overlay — level title, day/hour, and score across the first tile row.</item>
    ///   <item>Resource counts HUD — available line/train/wagon counts in the bottom-left column.</item>
    ///   <item>Player action overlay — short label in the bottom-right, only when a non-idle action occurred.</item>
    /// </list>
    /// </summary>
    private string Compose(Level level, GameSnapshot snapshot)
    {
        int width  = level.GridWidth  * TileSize;
        int height = level.GridHeight * TileSize;

        var waterSet = new HashSet<(int X, int Y)>(
            level.LevelData.WaterTiles.Select(w => (w.GridX, w.GridY)));

        var colorMap = BuildColorMap(level.LevelData);

        // Map each line's GUID to a display color, ordered by creation (OrderId).
        var lineColorMap = snapshot.Lines
            .OrderBy(line => line.OrderId)
            .Select((line, i) => (line.LineId, Color: LineColors[i % LineColors.Length]))
            .ToDictionary(x => x.LineId, x => x.Color);

        // Reverse-lookup: station GUID → grid location, needed to position lines.
        var stationLocations = snapshot.Stations
            .ToDictionary(kvp => kvp.Value.Id, kvp => kvp.Key);

        // Pre-compute the colorMap cache-key suffix once so tile loading doesn't rebuild it.
        var colorKeySuffix = colorMap.Count == 0 ? ""
            : "|" + string.Join(",", colorMap.Select(kv => $"{kv.Key}={kv.Value}"));

        // Pre-group passengers by station to avoid O(stations × passengers) filtering.
        var passengersByStation = snapshot.Passengers
            .Where(p => p.StationId.HasValue)
            .GroupBy(p => p.StationId!.Value)
            .ToDictionary(g => g.Key, g => (IList<Passenger>)g.ToList());

        // Pre-compute tile paths per line once; reused for every train on that line.
        var tilePathByLine = snapshot.Lines
            .Where(l => lineColorMap.ContainsKey(l.LineId))
            .ToDictionary(l => l.LineId, l => LinePathHelper.ComputeTilePath(l, stationLocations));

        // ── First pass: collect unique tile names for <defs> ──────────────────
        // Water tiles use an 8-directional neighbour analysis to select the correct
        // blended-edge SVG asset.  Grid-boundary positions are treated as water so that
        // the map edge blends naturally.
        var uniqueTileNames = new HashSet<string>();
        var gridWaterTiles  = new List<(int x, int y, string tileName)>();

        for (int y = 0; y < level.GridHeight; y++)
        {
            for (int x = 0; x < level.GridWidth; x++)
            {
                if (!waterSet.Contains((x, y))) continue;

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
                uniqueTileNames.Add(tileName);
                gridWaterTiles.Add((x, y, tileName));
            }
        }

        foreach (var (_, station) in snapshot.Stations)
            uniqueTileNames.Add(GetStationTileName(station.StationType));

        // ── Build SVG ─────────────────────────────────────────────────────────
        var sb = new StringBuilder(32768);
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\">");

        // ── Defs: define each unique tile shape once ───────────────────────────
        sb.Append("<defs>");
        foreach (var tileName in uniqueTileNames)
        {
            string cacheKey = tileName + colorKeySuffix;
            if (!_tileInnerSvgCache.TryGetValue(cacheKey, out var innerSvg))
            {
                innerSvg = LoadTileInnerSvg(tileName, colorMap);
                _tileInnerSvgCache[cacheKey] = innerSvg;
            }
            sb.Append($"<g id=\"t-{tileName}\">{innerSvg}</g>");
        }
        sb.Append("</defs>");

        // ── Pass 1: background as a single rect ───────────────────────────────
        string bgFill = colorMap.TryGetValue(SourceBackgroundColor, out var bgOverride)
            ? bgOverride : SourceBackgroundColor;
        sb.Append($"<rect width=\"{width}\" height=\"{height}\" style=\"fill:{bgFill}\"/>");

        // ── Pass 1b: water tiles via <use> ────────────────────────────────────
        foreach (var (x, y, tileName) in gridWaterTiles)
            sb.Append($"<use href=\"#t-{tileName}\" transform=\"translate({x * TileSize},{y * TileSize})\"/>");

        // ── Pass 2: metro lines beneath station tiles ─────────────────────────
        AppendLines(sb, snapshot, stationLocations, lineColorMap);

        // ── Pass 3: station tiles on top of lines ─────────────────────────────
        foreach (var (loc, station) in snapshot.Stations)
            sb.Append($"<use href=\"#t-{GetStationTileName(station.StationType)}\" transform=\"translate({loc.X * TileSize},{loc.Y * TileSize})\"/>");

        // ── Pass 4: waiting passengers above station icons ────────────────────
        AppendWaitingPassengers(sb, snapshot, passengersByStation);

        // ── Pass 5: trains on top of all terrain and station elements ─────────
        AppendVehicles(sb, snapshot, lineColorMap, tilePathByLine);

        // ── Pass 6: header HUD ────────────────────────────────────────────────
        AppendHeader(sb, width, level.Title, snapshot.Time, snapshot.Score);

        // ── Pass 7: resource counts HUD ───────────────────────────────────────
        AppendResourceCounts(sb, level.GridHeight, snapshot);

        // ── Pass 8: player action overlay ─────────────────────────────────────
        if (snapshot.LastAction is not null and not NoAction)
            AppendPlayerAction(sb, width, level.GridHeight, snapshot.LastAction);

        sb.Append("</svg>");
        return sb.ToString();
    }

    // ─── Header overlay ───────────────────────────────────────────────────────

    /// <summary>
    /// Appends a semi-transparent black bar spanning the full width of the first tile row
    /// with the level title and current day/hour left-aligned and the current score right-aligned.
    /// Text is emitted as SVG <c>&lt;text&gt;</c> elements with <c>dominant-baseline="central"</c>
    /// for consistent vertical centring across SVG viewers.
    /// </summary>
    private static void AppendHeader(StringBuilder sb, int totalWidth, string levelTitle, GameTime time, int score)
    {
        const float headerHeight = TileSize;
        const float fontSize = 13f;
        const float padding = 8f;

        sb.Append($"<rect x=\"0\" y=\"0\" width=\"{totalWidth}\" height=\"{(int)headerHeight}\" style=\"fill:{HudBgFill}\"/>");

        string dowAbbrev = time.DayOfWeek switch
        {
            DayOfWeek.Monday    => "mo",
            DayOfWeek.Tuesday   => "tu",
            DayOfWeek.Wednesday => "we",
            DayOfWeek.Thursday  => "th",
            DayOfWeek.Friday    => "fr",
            DayOfWeek.Saturday  => "sa",
            _                   => "su",
        };
        string dayHour  = $"{dowAbbrev} {time.Day} / {time.Hour:D2}:00";
        string leftText = XmlEscape($"{levelTitle}: {dayHour}");
        float  cy       = headerHeight / 2f;

        sb.Append(Invariant($"<text x=\"{padding:F1}\" y=\"{cy:F1}\" dominant-baseline=\"central\" font-size=\"{(int)fontSize}\" fill=\"white\" font-family=\"{HudFont}\">{leftText}</text>"));

        // text-anchor="end" handles right-alignment without requiring font metrics.
        string rightText = $"score: {score}";
        float  rightX    = totalWidth - padding;
        sb.Append(Invariant($"<text x=\"{rightX:F1}\" y=\"{cy:F1}\" text-anchor=\"end\" dominant-baseline=\"central\" font-size=\"{(int)fontSize}\" fill=\"white\" font-family=\"{HudFont}\">{rightText}</text>"));
    }

    // ─── Player action overlay ────────────────────────────────────────────────

    /// <summary>
    /// Appends a semi-transparent label at the bottom-right corner of the grid,
    /// occupying the last tile row.  Only rendered when the snapshot's
    /// <see cref="GameSnapshot.LastAction"/> is a non-idle, displayable action.
    /// The background width is estimated from character count × average glyph width.
    /// </summary>
    private static void AppendPlayerAction(StringBuilder sb, int totalWidth, int gridHeight, PlayerAction action)
    {
        string? text = DescribeAction(action);
        if (text is null) return;

        const float fontSize     = 13f;
        const float padding      = 8f;
        const float avgCharWidth = 7.5f; // Liberation Sans at 13px

        float bgWidth = text.Length * avgCharWidth + padding * 2f;
        float tileY   = (gridHeight - 1) * TileSize;
        float cy      = tileY + TileSize / 2f;
        float bgX     = totalWidth - bgWidth;
        float rightX  = totalWidth - padding;

        sb.Append(Invariant($"<rect x=\"{bgX:F1}\" y=\"{tileY}\" width=\"{bgWidth:F1}\" height=\"{TileSize}\" style=\"fill:{HudBgFill}\"/>"));
        sb.Append(Invariant($"<text x=\"{rightX:F1}\" y=\"{cy:F1}\" text-anchor=\"end\" dominant-baseline=\"central\" font-size=\"{(int)fontSize}\" fill=\"white\" font-family=\"{HudFont}\">{XmlEscape(text)}</text>"));
    }

    /// <summary>
    /// Maps a <see cref="PlayerAction"/> to a short human-readable label for the
    /// bottom-right HUD overlay.  Returns <see langword="null"/> for actions that
    /// should not be displayed (e.g. <see cref="NoAction"/> and unrecognised types).
    /// </summary>
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

    /// <summary>
    /// Appends a small SVG shape representing a passenger's destination station type.
    /// The icon shape mirrors the corresponding station shape so that players can
    /// visually match waiting passengers to their target stops at a glance.
    /// </summary>
    private static void AppendPassengerIcon(
        StringBuilder sb, string fill,
        StationType type, float x, float y, float size)
    {
        float r  = size / 2f;
        float cx = x + r;
        float cy = y + r;

        switch (type)
        {
            case StationType.Circle:
                sb.Append(Invariant($"<circle cx=\"{cx:F1}\" cy=\"{cy:F1}\" r=\"{r:F1}\" fill=\"{fill}\"/>"));
                break;

            case StationType.Rectangle:
                sb.Append(Invariant($"<rect x=\"{x:F1}\" y=\"{y:F1}\" width=\"{size:F1}\" height=\"{size:F1}\" fill=\"{fill}\"/>"));
                break;

            case StationType.Triangle:
                sb.Append(Invariant($"<polygon points=\"{cx:F1},{y:F1} {(x + size):F1},{(y + size):F1} {x:F1},{(y + size):F1}\" fill=\"{fill}\"/>"));
                break;

            case StationType.Diamond:
                sb.Append(Invariant($"<polygon points=\"{cx:F1},{y:F1} {(x + size):F1},{cy:F1} {cx:F1},{(y + size):F1} {x:F1},{cy:F1}\" fill=\"{fill}\"/>"));
                break;

            case StationType.Pentagon:
                sb.Append($"<polygon points=\"{MakeRegularPolygonPoints(cx, cy, r, 5, -MathF.PI / 2f)}\" fill=\"{fill}\"/>");
                break;

            case StationType.Star:
                sb.Append($"<polygon points=\"{MakeStarPoints(cx, cy, r, r * 0.45f, 5)}\" fill=\"{fill}\"/>");
                break;
        }
    }

    /// <summary>
    /// Returns the SVG <c>points</c> attribute string for a regular <paramref name="sides"/>-sided
    /// polygon centred at (<paramref name="cx"/>, <paramref name="cy"/>) with circumscribed radius
    /// <paramref name="r"/>.
    /// </summary>
    private static string MakeRegularPolygonPoints(float cx, float cy, float r, int sides, float startAngle)
    {
        var pts = new StringBuilder();
        for (int i = 0; i < sides; i++)
        {
            float angle = startAngle + i * (2f * MathF.PI / sides);
            if (i > 0) pts.Append(' ');
            pts.Append(Invariant($"{cx + r * MathF.Cos(angle):F1},{cy + r * MathF.Sin(angle):F1}"));
        }
        return pts.ToString();
    }

    /// <summary>
    /// Returns the SVG <c>points</c> attribute string for a <paramref name="points"/>-pointed star
    /// centred at (<paramref name="cx"/>, <paramref name="cy"/>).  Outer vertices are at radius
    /// <paramref name="outerR"/> and inner vertices are at radius <paramref name="innerR"/>.
    /// </summary>
    private static string MakeStarPoints(float cx, float cy, float outerR, float innerR, int points)
    {
        var pts = new StringBuilder();
        float startAngle = -MathF.PI / 2f;
        for (int i = 0; i < points * 2; i++)
        {
            float angle = startAngle + i * (MathF.PI / points);
            float r = i % 2 == 0 ? outerR : innerR;
            if (i > 0) pts.Append(' ');
            pts.Append(Invariant($"{cx + r * MathF.Cos(angle):F1},{cy + r * MathF.Sin(angle):F1}"));
        }
        return pts.ToString();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the pixel coordinates of the centre of the tile at grid location
    /// <paramref name="loc"/>.  Formula: <c>(loc.X · TileSize + TileSize/2, loc.Y · TileSize + TileSize/2)</c>.
    /// </summary>
    private static (float x, float y) StationCenter(Location loc)
        => (loc.X * TileSize + TileSize / 2f, loc.Y * TileSize + TileSize / 2f);

    /// <summary>
    /// Converts floating-point tile coordinates to the pixel centre of that tile.
    /// Formula: <c>pixel = tile · TileSize + TileSize/2</c>.
    /// </summary>
    private static (float px, float py) TileToPixel(float tx, float ty)
        => (tx * TileSize + TileSize / 2f, ty * TileSize + TileSize / 2f);

    /// <summary>
    /// Escapes <paramref name="text"/> so it is safe to embed inside an SVG text element.
    /// </summary>
    private static string XmlEscape(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    /// <summary>
    /// Builds the SVG colour replacement map for a level's visual theme.
    /// The baked-in SVG tile assets use <see cref="SourceBackgroundColor"/> and
    /// <see cref="SourceWaterColor"/> as placeholder colour strings.  When a level
    /// specifies custom colours, this map drives a plain text-replace in
    /// <see cref="LoadTileInnerSvg"/> before caching the inner markup.
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

    public void Dispose()
    {
        _tileInnerSvgCache.Clear();
    }

    /// <summary>
    /// Loads an SVG asset from disk, applies colour substitutions from
    /// <paramref name="colorMap"/> via plain-text replacement, then extracts and returns
    /// the inner markup of the SVG element (the content between the opening <c>&lt;svg&gt;</c>
    /// tag and the closing <c>&lt;/svg&gt;</c> tag), wrapped in a <c>&lt;g&gt;</c> element
    /// that preserves the root <c>style</c> attribute (fill-rule, clip-rule, etc.).
    ///
    /// If the exact tile file does not exist the method falls back to a simpler variant using
    /// <see cref="GetFallbackTileName"/>.  A second miss throws
    /// <see cref="FileNotFoundException"/> to surface missing asset configuration early.
    /// </summary>
    private string LoadTileInnerSvg(string tileName, Dictionary<string, string> colorMap)
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

        // Find the end of the opening <svg ...> tag (handles multi-attribute tags on one line).
        int svgTagStart = svgText.IndexOf("<svg", StringComparison.OrdinalIgnoreCase);
        if (svgTagStart < 0)
            throw new InvalidOperationException($"No <svg element found in tile: {path}");

        int tagEnd         = svgText.IndexOf('>', svgTagStart) + 1;
        int closeTagStart  = svgText.LastIndexOf("</svg>", StringComparison.OrdinalIgnoreCase);
        if (closeTagStart < 0) closeTagStart = svgText.Length;

        // Preserve fill-rule / clip-rule / stroke-* from the root <svg> style attribute.
        string openTag = svgText[svgTagStart..tagEnd];
        string style   = ExtractAttributeValue(openTag, "style");
        string inner   = svgText[tagEnd..closeTagStart].Trim();

        return string.IsNullOrEmpty(style)
            ? inner
            : $"<g style=\"{style}\">{inner}</g>";
    }

    private static string ExtractAttributeValue(string element, string attrName)
    {
        string search = $"{attrName}=\"";
        int start = element.IndexOf(search, StringComparison.OrdinalIgnoreCase);
        if (start < 0) return "";
        start += search.Length;
        int end = element.IndexOf('"', start);
        return end < 0 ? "" : element[start..end];
    }

    /// <summary>
    /// Falls back to the "all relevant diagonals water" version of the same cardinal combination.
    /// </summary>
    private static string? GetFallbackTileName(string tileName)
    {
        if (!tileName.StartsWith("water"))
            return null;

        var suffix = tileName == "water" ? "" : tileName["water-".Length..];
        var parts  = suffix.Length > 0 ? suffix.Split('-') : [];
        var dirs   = new HashSet<string>(parts);

        bool n = dirs.Contains("N"), e = dirs.Contains("E"),
             s = dirs.Contains("S"), w = dirs.Contains("W");

        return BuildWaterTileName(n, n && e, e, e && s, s, s && w, w, w && n);
    }

    /// <summary>
    /// Thin wrapper over <see cref="BuildWaterTileName"/> for call-site clarity.
    /// </summary>
    private static string GetWaterTileName(bool n, bool ne, bool e, bool se, bool s, bool sw, bool w, bool nw)
        => BuildWaterTileName(n, ne, e, se, s, sw, w, nw);

    /// <summary>
    /// Constructs the SVG asset filename for a water tile based on which of its
    /// 8 neighbours are also water (or grid edges).
    ///
    /// Cardinal directions (N, E, S, W) are included whenever the corresponding
    /// neighbour is water.  Diagonal directions (NE, SE, SW, NW) are only included
    /// when <em>both</em> flanking cardinal neighbours are also water.
    /// </summary>
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

    /// <summary>
    /// Appends all metro lines as SVG <c>&lt;line&gt;</c> elements with
    /// <c>stroke-linecap="round"</c> so segment joints look smooth.
    /// </summary>
    private static void AppendLines(
        StringBuilder sb,
        GameSnapshot snapshot,
        Dictionary<Guid, Location> stationLocations,
        Dictionary<Guid, string> lineColorMap)
    {
        foreach (var line in snapshot.Lines.OrderBy(l => l.OrderId))
        {
            if (!lineColorMap.TryGetValue(line.LineId, out var color)) continue;

            for (int i = 0; i < line.StationIds.Count - 1; i++)
            {
                if (!stationLocations.TryGetValue(line.StationIds[i],     out var locA)) continue;
                if (!stationLocations.TryGetValue(line.StationIds[i + 1], out var locB)) continue;

                var waypoints = LinePathHelper.ComputeSegmentWaypoints(locA, locB);
                for (int j = 0; j < waypoints.Count - 1; j++)
                {
                    var (ax, ay) = TileToPixel(waypoints[j].X,     waypoints[j].Y);
                    var (bx, by) = TileToPixel(waypoints[j + 1].X, waypoints[j + 1].Y);
                    sb.Append(Invariant($"<line x1=\"{ax:F1}\" y1=\"{ay:F1}\" x2=\"{bx:F1}\" y2=\"{by:F1}\" stroke=\"{color}\" stroke-width=\"{LineStrokeWidth}\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>"));
                }
            }
        }
    }

    // ─── Waiting passengers ───────────────────────────────────────────────────

    /// <summary>
    /// Appends small destination-type icons above each station tile to show how
    /// many passengers are waiting and where they want to go.
    ///
    /// Up to 20 passengers per station are shown (two rows of 10 each), stacked
    /// upward from just above the station tile.
    /// </summary>
    private static void AppendWaitingPassengers(
        StringBuilder sb,
        GameSnapshot snapshot,
        Dictionary<Guid, IList<Passenger>> passengersByStation)
    {
        const int    maxPerRow = 10;
        const string fill     = "#333333";

        foreach (var (loc, station) in snapshot.Stations)
        {
            if (!passengersByStation.TryGetValue(station.Id, out var passengers) || passengers.Count == 0) continue;

            var (cx, _)     = StationCenter(loc);
            float stationTopY = loc.Y * TileSize;

            int row1Count = Math.Min(passengers.Count, maxPerRow);
            int row2Count = Math.Min(passengers.Count - row1Count, maxPerRow);

            float row1TopY = stationTopY - WaitingPassengerSize - 2f;
            float row2TopY = row1TopY - (WaitingPassengerSize + PassengerGap);

            AppendPassengerRow(sb, fill, passengers, 0,          row1Count,              cx, row1TopY, WaitingPassengerSize);
            if (row2Count > 0)
                AppendPassengerRow(sb, fill, passengers, row1Count, row1Count + row2Count, cx, row2TopY, WaitingPassengerSize);
        }
    }

    /// <summary>
    /// Appends a single horizontal row of passenger destination icons centred on
    /// <paramref name="centerX"/>, starting at pixel Y = <paramref name="topY"/>.
    /// </summary>
    private static void AppendPassengerRow(
        StringBuilder sb, string fill,
        IList<Passenger> passengers, int start, int end,
        float centerX, float topY, float size)
    {
        int count = end - start;
        if (count <= 0) return;

        float totalWidth = count * size + (count - 1) * PassengerGap;
        float startX     = centerX - totalWidth / 2f;

        for (int i = 0; i < count; i++)
        {
            float px = startX + i * (size + PassengerGap);
            AppendPassengerIcon(sb, fill, passengers[start + i].DestinationType, px, topY, size);
        }
    }

    // ─── Vehicles ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Appends all trains in the snapshot as directional SVG groups containing the
    /// pre-defined train body path.  The facing angle is derived by looking at the
    /// next tile in the train's travel direction on the precomputed tile path.
    /// </summary>
    private static void AppendVehicles(
        StringBuilder sb,
        GameSnapshot snapshot,
        Dictionary<Guid, string> lineColorMap,
        Dictionary<Guid, List<Location>> tilePathByLine)
    {
        foreach (var train in snapshot.Trains)
        {
            if (!lineColorMap.TryGetValue(train.LineId, out var lineColor)) continue;
            if (!tilePathByLine.TryGetValue(train.LineId, out var tilePath) || tilePath.Count == 0) continue;

            var (vx, vy) = TileToPixel(train.TilePosition.X, train.TilePosition.Y);

            float angle        = 0f;
            int   currentIndex = (train.PathIndex >= 0
                                  && train.PathIndex < tilePath.Count
                                  && tilePath[train.PathIndex] == train.TilePosition)
                                 ? train.PathIndex
                                 : tilePath.IndexOf(train.TilePosition);
            if (currentIndex != -1 && tilePath.Count > 1)
            {
                int nextIndex = currentIndex + train.Direction;
                if (nextIndex < 0 || nextIndex >= tilePath.Count)
                    nextIndex = currentIndex - train.Direction;
                nextIndex = Math.Clamp(nextIndex, 0, tilePath.Count - 1);

                var nextTile = tilePath[nextIndex];
                angle = MathF.Atan2(
                    nextTile.Y - train.TilePosition.Y,
                    nextTile.X - train.TilePosition.X);
            }

            AppendVehicleRect(sb, vx, vy, angle, lineColor, train.Passengers);
        }
    }

    /// <summary>
    /// Appends a single train oriented along <paramref name="angleDeg"/> and centered at
    /// (<paramref name="cx"/>, <paramref name="cy"/>) using SVG <c>rotate()</c> transform.
    ///
    /// Shape: <see cref="TrainBodySvgPath"/> — a pointy-front rectangle with rounded rear corners.
    /// </summary>
    private static void AppendVehicleRect(
        StringBuilder sb, float cx, float cy, float angle,
        string lineColor, IReadOnlyList<Passenger> passengers)
    {
        float angleDeg = angle * (180f / MathF.PI);
        sb.Append(Invariant($"<g transform=\"translate({cx:F1},{cy:F1}) rotate({angleDeg:F1})\">"));
        sb.Append($"<path d=\"{TrainBodySvgPath}\" fill=\"{lineColor}\" stroke=\"white\" stroke-width=\"{TrainBorderWidth}\"/>");
        if (passengers.Count > 0)
            AppendPassengersInVehicle(sb, passengers);
        sb.Append("</g>");
    }

    /// <summary>
    /// Appends passenger icons inside the vehicle, arranged in a grid centred on the vehicle origin.
    /// </summary>
    private static void AppendPassengersInVehicle(StringBuilder sb, IReadOnlyList<Passenger> passengers)
    {
        const float padding = 2.0f;

        float innerW = TrainLength - padding * 2;
        float innerH = TrainHeight - padding * 2;

        int cols       = Math.Max(1, (int)((innerW + PassengerGap) / (TrainPassengerSize + PassengerGap)));
        int rows       = Math.Max(1, (int)((innerH + PassengerGap) / (TrainPassengerSize + PassengerGap)));
        int maxVisible = Math.Min(passengers.Count, cols * rows);

        int   actualCols = Math.Min(maxVisible, cols);
        int   actualRows = (int)MathF.Ceiling((float)maxVisible / actualCols);
        float gridW      = actualCols * TrainPassengerSize + (actualCols - 1) * PassengerGap;
        float gridH      = actualRows * TrainPassengerSize + (actualRows - 1) * PassengerGap;
        float startX     = -gridW / 2f;
        float startY     = -gridH / 2f;

        for (int i = 0; i < maxVisible; i++)
        {
            int   row = i / actualCols;
            int   col = i % actualCols;
            float px  = startX + col * (TrainPassengerSize + PassengerGap);
            float py  = startY + row * (TrainPassengerSize + PassengerGap);
            AppendPassengerIcon(sb, "white", passengers[i].DestinationType, px, py, TrainPassengerSize);
        }
    }

    // ─── Resource counts HUD ──────────────────────────────────────────────────

    /// <summary>
    /// Appends the available resource counts in the bottom three tiles of the first column.
    /// Each tile shows a Material Design resource icon scaled to 16×16 and the available count.
    /// </summary>
    private static void AppendResourceCounts(StringBuilder sb, int gridHeight, GameSnapshot snapshot)
    {
        int availableLines  = 0;
        int availableTrains = 0;
        int availableWagons = 0;
        foreach (var r in snapshot.Resources)
        {
            if (r.InUse) continue;
            if      (r.Type == ResourceType.Line)  availableLines++;
            else if (r.Type == ResourceType.Train) availableTrains++;
            else if (r.Type == ResourceType.Wagon) availableWagons++;
        }

        (int RowOffset, ResourceType Type, int Count, string IconPath)[] items =
        [
            (-3, ResourceType.Line,  availableLines,  LineIconSvgPath),
            (-2, ResourceType.Train, availableTrains, TrainIconSvgPath),
            (-1, ResourceType.Wagon, availableWagons, WagonIconSvgPath),
        ];

        const float iconSize = 16f;
        const float scale    = iconSize / 24f;
        const float fontSize = 11f;
        const float iconCx   = 10f;

        foreach (var (rowOffset, _, count, iconPath) in items)
        {
            int   row        = gridHeight + rowOffset;
            if (row < 0) continue;

            float tileY      = row * TileSize;
            float tileCenterY = tileY + TileSize / 2f;

            sb.Append(Invariant($"<rect x=\"0\" y=\"{tileY:F1}\" width=\"{TileSize}\" height=\"{TileSize}\" style=\"fill:{HudBgFill}\"/>"));

            float tx = iconCx - iconSize / 2f;
            float ty = tileCenterY - iconSize / 2f;
            sb.Append(Invariant($"<g transform=\"translate({tx:F1},{ty:F1}) scale({scale:F4})\"><path d=\"{iconPath}\" fill=\"white\"/></g>"));

            sb.Append(Invariant($"<text x=\"20\" y=\"{tileCenterY:F1}\" dominant-baseline=\"central\" font-size=\"{(int)fontSize}\" fill=\"white\" font-family=\"{HudFont}\">{count}</text>"));
        }
    }

    /// <summary>
    /// Maps a <see cref="StationType"/> to the SVG tile filename (without the
    /// <c>.svg</c> extension) used to draw that station on the grid.
    /// </summary>
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
