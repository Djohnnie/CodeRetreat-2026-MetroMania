using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;
using MetroMania.Engine.Tests.Support;
using Reqnroll;

namespace MetroMania.Engine.Tests.StepDefinitions;

[Binding]
public class WeeklyGiftDeterminismStepDefinitions(EngineTestContext ctx)
{
    [Given(@"an empty level with seed (\d+)")]
    public void GivenAnEmptyLevelWithSeed(int seed)
    {
        ctx.Seed = seed;
    }

    [Given(@"a weekly gift override for week (\d+) with resource type (\w+)")]
    public void GivenAWeeklyGiftOverride(int week, string resourceType)
    {
        ctx.WeeklyGiftOverrides.Add(new WeeklyGiftOverride
        {
            Week = week,
            ResourceType = Enum.Parse<ResourceType>(resourceType)
        });
    }

    [When(@"the simulation runs again for (\d+) hours? with the same seed")]
    public void WhenTheSimulationRunsAgainWithSameSeed(int hours)
    {
        ctx.PrepareForRerun();
        var level = ctx.BuildLevel();
        ctx.Snapshot = ctx.Engine.RunForHours(ctx.Runner.Object, level, hours);
    }

    [Then(@"both runs should have produced the same weekly gift sequence")]
    public void ThenBothRunsShouldHaveProducedSameSequence()
    {
        Assert.Equal(ctx.PreviousWeeklyGiftTypes, ctx.WeeklyGiftTypes);
    }

    [Then(@"at least (\d+) weekly gifts should have been produced per run")]
    public void ThenAtLeastNGiftsShouldHaveBeenProducedPerRun(int minimum)
    {
        Assert.True(ctx.PreviousWeeklyGiftTypes.Count >= minimum || ctx.WeeklyGiftTypes.Count >= minimum,
            $"Expected at least {minimum} gifts, got {ctx.WeeklyGiftTypes.Count}");
    }

    [Then(@"weekly gift (\d+) should be (\w+)")]
    public void ThenWeeklyGiftNShouldBe(int position, string resourceType)
    {
        var expected = Enum.Parse<ResourceType>(resourceType);
        Assert.True(ctx.WeeklyGiftTypes.Count >= position,
            $"Expected at least {position} weekly gifts, got {ctx.WeeklyGiftTypes.Count}");
        Assert.Equal(expected, ctx.WeeklyGiftTypes[position - 1]);
    }
}
