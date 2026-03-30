using MetroMania.Engine.Tests.Support;
using Reqnroll;

namespace MetroMania.Engine.Tests.StepDefinitions;

[Binding]
public class MaxDaysSimulationStepDefinitions(EngineTestContext ctx)
{
    [Given(@"the level has a max days limit of (\d+) days?")]
    public void GivenTheLevelHasAMaxDaysLimitOf(int days)
    {
        ctx.MaxDaysOverride = days;
    }

    [Then(@"the result should show exactly (\d+) days survived")]
    public void ThenResultShowsExactlyNDaysSurvived(int days)
    {
        Assert.NotNull(ctx.Result);
        Assert.Equal(days, ctx.Result.DaysSurvived);
    }

    [Then(@"the simulation should have run exactly (\d+) hour ticks?")]
    public void ThenSimulationRanExactlyNHourTicks(int hours)
    {
        Assert.Equal(hours, ctx.HourTickCalls.Count);
    }
}
