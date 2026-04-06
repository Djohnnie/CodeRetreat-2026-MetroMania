using MetroMania.Domain.Enums;
using MetroMania.Engine.Model;
using MetroMania.Engine.Tests.Support;
using Reqnroll;

namespace MetroMania.Engine.Tests.StepDefinitions;

/// <summary>
/// Step definitions for multi-line and multi-train scenarios needed
/// for collision resolution and transfer/routing coverage tests.
/// </summary>
[Binding]
public class MultiLineStepDefinitions(EngineTestContext ctx)
{
    [Given(@"the runner will create a second line between stations at \((\d+),(\d+)\) and \((\d+),(\d+)\)")]
    public void GivenRunnerCreateSecondLine(int x1, int y1, int x2, int y2)
    {
        ctx.PendingActions.Enqueue(snapshot =>
        {
            // Find the second unused line resource
            var lineResource = snapshot.Resources
                .Where(r => r.Type == ResourceType.Line && !r.InUse)
                .FirstOrDefault();
            if (lineResource is null) return PlayerAction.None;

            if (!snapshot.Stations.TryGetValue(new Location(x1, y1), out var s1) ||
                !snapshot.Stations.TryGetValue(new Location(x2, y2), out var s2))
                return PlayerAction.None;

            return new CreateLine(lineResource.Id, s1.Id, s2.Id);
        });
    }

    [Given(@"the runner will extend line (\d+) from station \((\d+),(\d+)\) to station \((\d+),(\d+)\)")]
    public void GivenRunnerExtendLineN(int lineNumber, int x1, int y1, int x2, int y2)
    {
        ctx.PendingActions.Enqueue(snapshot =>
        {
            int index = lineNumber - 1;
            if (index >= snapshot.Lines.Count) return PlayerAction.None;
            var line = snapshot.Lines[index];

            if (!snapshot.Stations.TryGetValue(new Location(x1, y1), out var fromStation) ||
                !snapshot.Stations.TryGetValue(new Location(x2, y2), out var toStation))
                return PlayerAction.None;

            return new ExtendLineFromTerminal(line.LineId, fromStation.Id, toStation.Id);
        });
    }

    [Given(@"the runner will deploy a train on line (\d+) at station \((\d+),(\d+)\)")]
    public void GivenRunnerDeployTrainOnLineN(int lineNumber, int x, int y)
    {
        ctx.PendingActions.Enqueue(snapshot =>
        {
            var trainResource = snapshot.Resources.FirstOrDefault(r => r.Type == ResourceType.Train && !r.InUse);
            int index = lineNumber - 1;
            if (trainResource is null || index >= snapshot.Lines.Count) return PlayerAction.None;
            var line = snapshot.Lines[index];

            if (!snapshot.Stations.TryGetValue(new Location(x, y), out var station))
                return PlayerAction.None;

            return new AddVehicleToLine(trainResource.Id, line.LineId, station.Id);
        });
    }

    [Then(@"no two trains should occupy the same station tile")]
    public void ThenNoTwoTrainsAtSameStation()
    {
        Assert.NotNull(ctx.LastSnapshot);
        var stationTiles = new HashSet<Location>(ctx.LastSnapshot.Stations.Keys);
        var trainStationTiles = ctx.LastSnapshot.Trains
            .Select(t => t.TilePosition)
            .Where(pos => stationTiles.Contains(pos))
            .ToList();
        Assert.Equal(trainStationTiles.Count, trainStationTiles.Distinct().Count());
    }

    [Then(@"train (\d+) should not be at tile \((\d+),(\d+)\)")]
    public void ThenTrainNNotAtTile(int trainNumber, int x, int y)
    {
        Assert.NotNull(ctx.LastSnapshot);
        int index = trainNumber - 1;
        Assert.True(index < ctx.LastSnapshot.Trains.Count,
            $"Expected at least {trainNumber} train(s) but found {ctx.LastSnapshot.Trains.Count}");
        Assert.NotEqual(new Location(x, y), ctx.LastSnapshot.Trains[index].TilePosition);
    }

    [Then(@"train (\d+) should have (\d+) passengers? on board")]
    public void ThenTrainNPassengerCount(int trainNumber, int expected)
    {
        Assert.NotNull(ctx.LastSnapshot);
        int index = trainNumber - 1;
        Assert.True(index < ctx.LastSnapshot.Trains.Count);
        Assert.Equal(expected, ctx.LastSnapshot.Trains[index].Passengers.Count);
    }
}
