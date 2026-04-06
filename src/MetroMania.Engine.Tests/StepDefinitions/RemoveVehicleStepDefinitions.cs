using MetroMania.Domain.Enums;
using MetroMania.Engine.Model;
using MetroMania.Engine.Tests.Support;
using Reqnroll;

namespace MetroMania.Engine.Tests.StepDefinitions;

[Binding]
public class RemoveVehicleStepDefinitions(EngineTestContext ctx)
{
    [Then(@"the first train should have pending removal")]
    public void ThenFirstTrainShouldHavePendingRemoval()
    {
        var train = ctx.LastSnapshot!.Trains.First();
        Assert.True(train.PendingRemoval, "Expected the first train to have PendingRemoval = true.");
    }

    [Given(@"the runner will redeploy the released train on the first line at station \((\d+),(\d+)\)")]
    public void GivenRunnerRedeployReleasedTrain(int x, int y)
    {
        ctx.PendingActions.Enqueue(snapshot =>
        {
            var trainResource = snapshot.Resources.FirstOrDefault(r => r.Type == ResourceType.Train && !r.InUse);
            var line = snapshot.Lines.FirstOrDefault();
            if (trainResource is null || line is null) return PlayerAction.None;

            if (!snapshot.Stations.TryGetValue(new Location(x, y), out var station))
                return PlayerAction.None;

            return new AddVehicleToLine(trainResource.Id, line.LineId, station.Id);
        });
    }
}
