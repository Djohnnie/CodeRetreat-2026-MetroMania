using MetroMania.Engine.Model;

namespace MetroMania.Engine;

/// <summary>
/// Computes the ordered sequence of tile positions that make up a metro line's full route.
/// Used by the engine for train movement; tiles match exactly what the renderer draws.
/// </summary>
internal static class LinePathHelper
{
    /// <summary>
    /// Returns the inflection-point waypoints for a single segment between two station locations.
    /// The path uses at most one 45 degree diagonal; any remaining straight portion is split evenly
    /// between the start and end.  All coordinates are integer tile-grid positions.
    /// This is the canonical path shape shared with the renderer.
    /// </summary>
    public static List<Location> ComputeSegmentWaypoints(Location a, Location b)
    {
        int dx = b.X - a.X;
        int dy = b.Y - a.Y;
        int absDx = Math.Abs(dx);
        int absDy = Math.Abs(dy);
        int sdx = Math.Sign(dx);
        int sdy = Math.Sign(dy);
        int diagLen = Math.Min(absDx, absDy);
        int startStraight = Math.Abs(absDx - absDy) / 2;

        int x0 = a.X, y0 = a.Y;
        int x3 = b.X, y3 = b.Y;
        int x1, y1, x2, y2;

        if (absDx >= absDy) // horizontal-dominant: straight portions are horizontal
        {
            x1 = x0 + sdx * startStraight; y1 = y0;
            x2 = x1 + sdx * diagLen;       y2 = y1 + sdy * diagLen;
        }
        else // vertical-dominant: straight portions are vertical
        {
            x1 = x0;                        y1 = y0 + sdy * startStraight;
            x2 = x1 + sdx * diagLen;        y2 = y1 + sdy * diagLen;
        }

        var pts = new List<Location> { new(x0, y0) };
        if (x1 != x0 || y1 != y0) pts.Add(new(x1, y1));
        if (x2 != x1 || y2 != y1) pts.Add(new(x2, y2));
        var last = pts[^1];
        if (x3 != last.X || y3 != last.Y) pts.Add(new(x3, y3));

        return pts;
    }

    /// <summary>
    /// Returns every tile (grid coordinate) a train visits when traversing the full line,
    /// from the first station to the last.  Each consecutive pair of tiles is exactly one
    /// step apart — horizontal, vertical, or 45 degree diagonal.
    /// Uses <see cref="ComputeSegmentWaypoints"/> so the path is identical to what the renderer draws.
    /// </summary>
    public static List<Location> ComputeTilePath(
        Line line,
        IReadOnlyDictionary<Guid, Location> stationLocations)
    {
        var path = new List<Location>();

        for (int i = 0; i < line.StationIds.Count - 1; i++)
        {
            if (!stationLocations.TryGetValue(line.StationIds[i], out var from)) continue;
            if (!stationLocations.TryGetValue(line.StationIds[i + 1], out var to)) continue;

            var waypoints = ComputeSegmentWaypoints(from, to);

            for (int w = 0; w < waypoints.Count - 1; w++)
            {
                var wp0 = waypoints[w];
                var wp1 = waypoints[w + 1];

                if (path.Count == 0 || path[^1] != wp0)
                    path.Add(wp0);

                // Walk step-by-step between consecutive waypoints (H, V, or 45 degree diagonal)
                int x = wp0.X, y = wp0.Y;
                while (x != wp1.X || y != wp1.Y)
                {
                    x += Math.Sign(wp1.X - x);
                    y += Math.Sign(wp1.Y - y);
                    path.Add(new Location(x, y));
                }
            }
        }

        if (path.Count == 0 && line.StationIds.Count == 1 &&
            stationLocations.TryGetValue(line.StationIds[0], out var solo))
            path.Add(solo);

        return path;
    }
}
