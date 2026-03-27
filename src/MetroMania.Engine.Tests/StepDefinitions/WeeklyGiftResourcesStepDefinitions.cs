using MetroMania.Domain.Enums;
using MetroMania.Engine.Tests.Support;
using Reqnroll;

namespace MetroMania.Engine.Tests.StepDefinitions;

[Binding]
public class WeeklyGiftResourcesStepDefinitions(EngineTestContext ctx)
{
    [Then(@"the snapshot should contain at least (\d+) available resources? of type (\w+)")]
    public void ThenSnapshotShouldContainAtLeastNAvailableResourcesOfType(int minimum, string resourceType)
    {
        Assert.NotNull(ctx.Snapshot);
        var type = Enum.Parse<ResourceType>(resourceType);
        int count = ctx.Snapshot.Resources.Count(r => r.Type == type && !r.InUse);
        Assert.True(count >= minimum,
            $"Expected at least {minimum} available {resourceType} resource(s), found {count}");
    }

    [Then(@"the total resource count should be the initial 2 plus the number of weekly gifts received")]
    public void ThenTotalResourceCountShouldBeInitialPlusGifts()
    {
        Assert.NotNull(ctx.Snapshot);
        int expected = 2 + ctx.WeeklyGiftTypes.Count;
        Assert.Equal(expected, ctx.Snapshot.Resources.Count);
    }

    [Then(@"all resources in the snapshot should be available")]
    public void ThenAllResourcesShouldBeAvailable()
    {
        Assert.NotNull(ctx.Snapshot);
        Assert.All(ctx.Snapshot.Resources, r =>
            Assert.False(r.InUse, $"Resource {r.Id} ({r.Type}) should not be in use"));
    }

    [Then(@"all resources in the snapshot should have unique Ids")]
    public void ThenAllResourcesShouldHaveUniqueIds()
    {
        Assert.NotNull(ctx.Snapshot);
        var ids = ctx.Snapshot.Resources.Select(r => r.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Then(@"each weekly gift snapshot should contain the gifted resource as available")]
    public void ThenEachWeeklyGiftSnapshotShouldContainGiftedResource()
    {
        Assert.NotEmpty(ctx.WeeklyGiftEvents);
        foreach (var (snapshot, gift) in ctx.WeeklyGiftEvents)
        {
            int availableOfType = snapshot.Resources.Count(r => r.Type == gift && !r.InUse);
            Assert.True(availableOfType >= 1,
                $"Weekly gift snapshot at Day {snapshot.Time.Day} should contain at least 1 available {gift} resource, found {availableOfType}");
        }
    }

    [Then(@"the resource count should grow by 1 with each weekly gift")]
    public void ThenResourceCountShouldGrowByOneWithEachGift()
    {
        Assert.NotEmpty(ctx.WeeklyGiftEvents);
        for (int i = 0; i < ctx.WeeklyGiftEvents.Count; i++)
        {
            int expectedCount = 2 + (i + 1);
            int actualCount = ctx.WeeklyGiftEvents[i].Snapshot.Resources.Count;
            Assert.Equal(expectedCount, actualCount);
        }
    }
}
