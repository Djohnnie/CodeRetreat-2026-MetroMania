using MetroMania.Domain.Enums;
using MetroMania.Engine.Tests.Support;
using Reqnroll;

namespace MetroMania.Engine.Tests.StepDefinitions;

[Binding]
public class WeeklyGiftStepDefinitions(EngineTestContext ctx)
{
    [Then(@"weekly gift (\d+) should be of type (\w+)")]
    public void ThenWeeklyGiftNIsOfType(int giftNumber, string resourceTypeName)
    {
        Assert.True(ctx.WeeklyGiftCalls.Count >= giftNumber,
            $"Expected at least {giftNumber} weekly gift(s) but got {ctx.WeeklyGiftCalls.Count}");
        var expected = Enum.Parse<ResourceType>(resourceTypeName);
        Assert.Equal(expected, ctx.WeeklyGiftCalls[giftNumber - 1].Gift);
    }

    [Then(@"all weekly gifts should be of type Line or Train")]
    public void ThenAllWeeklyGiftsAreLineOrTrain()
    {
        Assert.NotEmpty(ctx.WeeklyGiftCalls);
        Assert.All(ctx.WeeklyGiftCalls, gift =>
            Assert.True(
                gift.Gift == ResourceType.Line || gift.Gift == ResourceType.Train,
                $"Expected gift to be Line or Train but was {gift.Gift}"));
    }

    [Then(@"both runs should have produced the same weekly gift sequence")]
    public void ThenBothRunsProducedSameGiftSequence()
    {
        Assert.NotEmpty(ctx.PreviousWeeklyGifts);
        Assert.NotEmpty(ctx.WeeklyGiftCalls);
        Assert.Equal(ctx.PreviousWeeklyGifts.Count, ctx.WeeklyGiftCalls.Count);
        for (int i = 0; i < ctx.PreviousWeeklyGifts.Count; i++)
            Assert.Equal(ctx.PreviousWeeklyGifts[i], ctx.WeeklyGiftCalls[i].Gift);
    }

    [Then(@"weekly gift (\d+) should have been received on day (\d+) at hour (\d+)")]
    public void ThenWeeklyGiftNReceivedOnDayAtHour(int giftNumber, int day, int hour)
    {
        Assert.True(ctx.WeeklyGiftCalls.Count >= giftNumber,
            $"Expected at least {giftNumber} weekly gift(s) but got {ctx.WeeklyGiftCalls.Count}");
        var gift = ctx.WeeklyGiftCalls[giftNumber - 1];
        Assert.Equal(day, gift.Time.Day);
        Assert.Equal(hour, gift.Time.Hour);
    }
}
