using MetroMania.Domain.Enums;
using MetroMania.Engine.Tests.Support;
using Reqnroll;

namespace MetroMania.Engine.Tests.StepDefinitions;

/// <summary>
/// Step definitions for asserting resource state (InUse, counts by type).
/// </summary>
[Binding]
public class ResourceStepDefinitions(EngineTestContext ctx)
{
    [Then(@"all initial resources should not be in use")]
    public void ThenAllInitialResourcesNotInUse()
    {
        Assert.NotNull(ctx.SimResult);
        var firstSnapshot = ctx.SimResult.GameSnapshots[0];
        Assert.All(firstSnapshot.Resources, r => Assert.False(r.InUse,
            $"Resource {r.Id} (type {r.Type}) should not be in use in the first snapshot."));
    }

    [Then(@"there should be (\d+) unused Line resources?")]
    public void ThenUnusedLineResources(int expected)
    {
        Assert.NotNull(ctx.LastSnapshot);
        var count = ctx.LastSnapshot.Resources.Count(r => r.Type == ResourceType.Line && !r.InUse);
        Assert.Equal(expected, count);
    }

    [Then(@"there should be (\d+) unused Train resources?")]
    public void ThenUnusedTrainResources(int expected)
    {
        Assert.NotNull(ctx.LastSnapshot);
        var count = ctx.LastSnapshot.Resources.Count(r => r.Type == ResourceType.Train && !r.InUse);
        Assert.Equal(expected, count);
    }

    [Then(@"there should be (\d+) in-use Line resources?")]
    public void ThenInUseLineResources(int expected)
    {
        Assert.NotNull(ctx.LastSnapshot);
        var count = ctx.LastSnapshot.Resources.Count(r => r.Type == ResourceType.Line && r.InUse);
        Assert.Equal(expected, count);
    }

    [Then(@"there should be (\d+) in-use Train resources?")]
    public void ThenInUseTrainResources(int expected)
    {
        Assert.NotNull(ctx.LastSnapshot);
        var count = ctx.LastSnapshot.Resources.Count(r => r.Type == ResourceType.Train && r.InUse);
        Assert.Equal(expected, count);
    }

    [Then(@"all resources should have unique IDs")]
    public void ThenAllResourcesUniqueIds()
    {
        Assert.NotNull(ctx.LastSnapshot);
        var ids = ctx.LastSnapshot.Resources.Select(r => r.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }
}
