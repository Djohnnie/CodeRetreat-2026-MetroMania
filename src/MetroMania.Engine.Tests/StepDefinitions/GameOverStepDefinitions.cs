using MetroMania.Engine.Model;
using MetroMania.Engine.Tests.Support;
using Reqnroll;

namespace MetroMania.Engine.Tests.StepDefinitions;

[Binding]
public class GameOverStepDefinitions(EngineTestContext ctx)
{
    [Then(@"the first overrun event should have (\d+) passengers at \((\d+),(\d+)\)")]
    public void ThenFirstOverrunEventShouldHavePassengers(int count, int x, int y)
    {
        Assert.NotEmpty(ctx.OverrunCalls);
        var first = ctx.OverrunCalls[0];
        Assert.Equal(count, first.PassengerCount);
        Assert.Equal(new Location(x, y), first.Location);
    }

    [Then(@"the game over event should have (\d+) passengers at \((\d+),(\d+)\)")]
    public void ThenGameOverEventShouldHavePassengers(int count, int x, int y)
    {
        Assert.Single(ctx.GameOverCalls);
        var gameOver = ctx.GameOverCalls[0];
        Assert.Equal(count, gameOver.PassengerCount);
        Assert.Equal(new Location(x, y), gameOver.Location);
    }
}
