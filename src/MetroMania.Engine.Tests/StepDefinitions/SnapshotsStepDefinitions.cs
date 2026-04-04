using MetroMania.Engine.Tests.Support;
using Reqnroll;

namespace MetroMania.Engine.Tests.StepDefinitions;

[Binding]
public class SnapshotsStepDefinitions(EngineTestContext ctx)
{
    [Then(@"the first snapshot should have score (\d+)")]
    public void ThenFirstSnapshotScore(int expected)
    {
        Assert.NotNull(ctx.SimResult);
        Assert.NotEmpty(ctx.SimResult.GameSnapshots);
        Assert.Equal(expected, ctx.SimResult.GameSnapshots[0].Score);
    }

    [Then(@"snapshot at index (\d+) should have TotalHoursElapsed of (\d+)")]
    public void ThenSnapshotAtIndexTotalHours(int index, int expected)
    {
        Assert.NotNull(ctx.SimResult);
        Assert.True(index < ctx.SimResult.GameSnapshots.Count,
            $"Expected at least {index + 1} snapshots but got {ctx.SimResult.GameSnapshots.Count}");
        Assert.Equal(expected, ctx.SimResult.GameSnapshots[index].TotalHoursElapsed);
    }
}
