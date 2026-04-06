using MetroMania.Engine.Model;
using MetroMania.Engine.Tests.Support;
using Reqnroll;

namespace MetroMania.Engine.Tests.StepDefinitions;

/// <summary>
/// Step definitions for LinePathHelper coverage: vertical-dominant segments,
/// single-station lines, and line tile path assertions.
/// </summary>
[Binding]
public class PathEdgeCaseStepDefinitions(EngineTestContext ctx)
{
    [Then(@"the line tile path should contain tile \((\d+),(\d+)\)")]
    public void ThenLineTilePathContainsTile(int x, int y)
    {
        Assert.NotNull(ctx.LastSnapshot);
        var line = ctx.LastSnapshot.Lines.FirstOrDefault();
        Assert.NotNull(line);

        var stationLocations = ctx.LastSnapshot.Stations
            .ToDictionary(kvp => kvp.Value.Id, kvp => kvp.Key);
        var tilePath = LinePathHelper.ComputeTilePath(line, stationLocations);

        Assert.Contains(new Location(x, y), tilePath);
    }

    [Then(@"the line tile path should have (\d+) tiles")]
    public void ThenLineTilePathHasNTiles(int expected)
    {
        Assert.NotNull(ctx.LastSnapshot);
        var line = ctx.LastSnapshot.Lines.FirstOrDefault();
        Assert.NotNull(line);

        var stationLocations = ctx.LastSnapshot.Stations
            .ToDictionary(kvp => kvp.Value.Id, kvp => kvp.Key);
        var tilePath = LinePathHelper.ComputeTilePath(line, stationLocations);

        Assert.Equal(expected, tilePath.Count);
    }

    [Then(@"the line tile path should include a diagonal segment")]
    public void ThenLineTilePathIncludesDiagonal()
    {
        Assert.NotNull(ctx.LastSnapshot);
        var line = ctx.LastSnapshot.Lines.FirstOrDefault();
        Assert.NotNull(line);

        var stationLocations = ctx.LastSnapshot.Stations
            .ToDictionary(kvp => kvp.Value.Id, kvp => kvp.Key);
        var tilePath = LinePathHelper.ComputeTilePath(line, stationLocations);

        bool hasDiagonal = false;
        for (int i = 0; i < tilePath.Count - 1; i++)
        {
            int dx = Math.Abs(tilePath[i + 1].X - tilePath[i].X);
            int dy = Math.Abs(tilePath[i + 1].Y - tilePath[i].Y);
            if (dx == 1 && dy == 1)
            {
                hasDiagonal = true;
                break;
            }
        }
        Assert.True(hasDiagonal, "Expected the tile path to include at least one diagonal step");
    }

    [Then(@"the line tile path should include a vertical straight segment")]
    public void ThenLineTilePathIncludesVerticalStraight()
    {
        Assert.NotNull(ctx.LastSnapshot);
        var line = ctx.LastSnapshot.Lines.FirstOrDefault();
        Assert.NotNull(line);

        var stationLocations = ctx.LastSnapshot.Stations
            .ToDictionary(kvp => kvp.Value.Id, kvp => kvp.Key);
        var tilePath = LinePathHelper.ComputeTilePath(line, stationLocations);

        bool hasVertical = false;
        for (int i = 0; i < tilePath.Count - 1; i++)
        {
            int dx = Math.Abs(tilePath[i + 1].X - tilePath[i].X);
            int dy = Math.Abs(tilePath[i + 1].Y - tilePath[i].Y);
            if (dx == 0 && dy == 1)
            {
                hasVertical = true;
                break;
            }
        }
        Assert.True(hasVertical, "Expected the tile path to include at least one vertical straight step");
    }
}
