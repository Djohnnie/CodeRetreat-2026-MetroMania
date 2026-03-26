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
        Assert.True(ctx.PreviousWeeklyGiftTypes.Count >= minimum,
            $"Expected at least {minimum} gifts in first run, got {ctx.PreviousWeeklyGiftTypes.Count}");
        Assert.True(ctx.WeeklyGiftTypes.Count >= minimum,
            $"Expected at least {minimum} gifts in second run, got {ctx.WeeklyGiftTypes.Count}");
    }
}
