using MetroMania.Engine.Model;
using MetroMania.Engine.Tests.Support;
using Reqnroll;

namespace MetroMania.Engine.Tests.StepDefinitions;

[Binding]
public class PassengerPickupStepDefinitions(EngineTestContext ctx)
{
    [Then(@"the first train should have (\d+) passengers? on board")]
    public void ThenFirstTrainPassengerCount(int expected)
    {
        Assert.NotNull(ctx.LastSnapshot);
        var train = ctx.LastSnapshot.Trains.FirstOrDefault();
        Assert.NotNull(train);
        Assert.Equal(expected, train.Passengers.Count);
    }

    [Then(@"there should be (\d+) passengers? waiting at station \((\d+),(\d+)\)")]
    public void ThenWaitingPassengersAtStation(int expected, int x, int y)
    {
        Assert.NotNull(ctx.LastSnapshot);
        var loc = new Location(x, y);
        if (!ctx.LastSnapshot.Stations.TryGetValue(loc, out var station))
        {
            Assert.Equal(0, expected);
            return;
        }
        int actual = ctx.LastSnapshot.Passengers.Count(p => p.StationId == station.Id);
        Assert.Equal(expected, actual);
    }
}
