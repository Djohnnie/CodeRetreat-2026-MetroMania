using MetroMania.Domain.Enums;
using MetroMania.Engine.Model;
using MetroMania.Engine.Tests.Support;
using Reqnroll;

namespace MetroMania.Engine.Tests.StepDefinitions;

/// <summary>
/// Step definitions for queuing deferred player actions.
/// Each step enqueues a factory lambda into <see cref="EngineTestContext.PendingActions"/>.
/// The queue is consumed one-per-tick from the OnHourTicked callback, so the first
/// "runner will …" step fires on tick 0, the second on tick 1, and so on.
/// </summary>
[Binding]
public class PlayerActionsStepDefinitions(EngineTestContext ctx)
{
    [Given(@"the runner will create a line between stations at \((\d+),(\d+)\) and \((\d+),(\d+)\)")]
    public void GivenRunnerCreateLine(int x1, int y1, int x2, int y2)
    {
        ctx.PendingActions.Enqueue(snapshot =>
        {
            var lineResource = snapshot.Resources.FirstOrDefault(r => r.Type == ResourceType.Line && !r.InUse);
            if (lineResource is null) return PlayerAction.None;

            if (!snapshot.Stations.TryGetValue(new Location(x1, y1), out var s1) ||
                !snapshot.Stations.TryGetValue(new Location(x2, y2), out var s2))
                return PlayerAction.None;

            return new CreateLine(lineResource.Id, s1.Id, s2.Id);
        });
    }

    [Given(@"the runner will extend the first line from station \((\d+),(\d+)\) to station \((\d+),(\d+)\)")]
    public void GivenRunnerExtendLine(int x1, int y1, int x2, int y2)
    {
        ctx.PendingActions.Enqueue(snapshot =>
        {
            var line = snapshot.Lines.FirstOrDefault();
            if (line is null) return PlayerAction.None;

            if (!snapshot.Stations.TryGetValue(new Location(x1, y1), out var fromStation) ||
                !snapshot.Stations.TryGetValue(new Location(x2, y2), out var toStation))
                return PlayerAction.None;

            return new ExtendLineFromTerminal(line.LineId, fromStation.Id, toStation.Id);
        });
    }

    [Given(@"the runner will deploy a train on the first line at station \((\d+),(\d+)\)")]
    public void GivenRunnerDeployTrain(int x, int y)
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

    [Given(@"the runner will do nothing on the next tick")]
    public void GivenRunnerDoNothing() => ctx.PendingActions.Enqueue(_ => PlayerAction.None);

    [Given(@"the runner will deploy a train on the last line at station \((\d+),(\d+)\)")]
    public void GivenRunnerDeployTrainOnLastLine(int x, int y)
    {
        ctx.PendingActions.Enqueue(snapshot =>
        {
            var trainResource = snapshot.Resources.FirstOrDefault(r => r.Type == ResourceType.Train && !r.InUse);
            var line = snapshot.Lines.LastOrDefault();
            if (trainResource is null || line is null) return PlayerAction.None;

            if (!snapshot.Stations.TryGetValue(new Location(x, y), out var station))
                return PlayerAction.None;

            return new AddVehicleToLine(trainResource.Id, line.LineId, station.Id);
        });
    }
}
