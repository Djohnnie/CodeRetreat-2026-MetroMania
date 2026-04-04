using MetroMania.Engine.Model;
using MetroMania.Engine.Tests.Support;
using Reqnroll;

namespace MetroMania.Engine.Tests.StepDefinitions;

[Binding]
public class TrainsMovingStepDefinitions(EngineTestContext ctx)
{
    [Then(@"the train should be at tile \((\d+),(\d+)\)")]
    public void ThenTrainAtTile(int x, int y)
    {
        Assert.NotNull(ctx.LastSnapshot);
        var train = ctx.LastSnapshot.Trains.FirstOrDefault();
        Assert.NotNull(train);
        Assert.Equal(new Location(x, y), train.TilePosition);
    }

    [Then(@"train (\d+) should be at tile \((\d+),(\d+)\)")]
    public void ThenTrainNAtTile(int trainNumber, int x, int y)
    {
        Assert.NotNull(ctx.LastSnapshot);
        int index = trainNumber - 1;
        Assert.True(index < ctx.LastSnapshot.Trains.Count,
            $"Expected at least {trainNumber} train(s) but found {ctx.LastSnapshot.Trains.Count}");
        Assert.Equal(new Location(x, y), ctx.LastSnapshot.Trains[index].TilePosition);
    }
}
