using MetroMania.Domain.Enums;
using MetroMania.Engine.Model;
using MetroMania.Engine.Tests.Support;
using Reqnroll;

namespace MetroMania.Engine.Tests.StepDefinitions;

/// <summary>
/// Step definitions for queuing player actions that are expected to be rejected
/// by the engine with specific error codes. Each step enqueues a factory lambda
/// that deliberately constructs an invalid action.
/// </summary>
[Binding]
public class ValidationActionsStepDefinitions(EngineTestContext ctx)
{
    // ═══════════════════════════════════════════════════════════════════
    // CreateLine error-triggering steps
    // ═══════════════════════════════════════════════════════════════════

    [Given(@"the runner will attempt to create a line with a non-existent resource")]
    public void GivenAttemptCreateLineNonExistentResource()
    {
        ctx.PendingActions.Enqueue(_ => new CreateLine(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
    }

    [Given(@"the runner will attempt to create a line from station \((\d+),(\d+)\) to itself")]
    public void GivenAttemptCreateLineSameStation(int x, int y)
    {
        ctx.PendingActions.Enqueue(snapshot =>
        {
            var lineResource = snapshot.Resources.FirstOrDefault(r => r.Type == ResourceType.Line && !r.InUse);
            if (lineResource is null) return PlayerAction.None;
            var station = snapshot.Stations[new Location(x, y)];
            return new CreateLine(lineResource.Id, station.Id, station.Id);
        });
    }

    [Given(@"the runner will attempt to extend the first line from non-terminal \((\d+),(\d+)\) to \((\d+),(\d+)\)")]
    public void GivenAttemptExtendFromNonTerminal(int x1, int y1, int x2, int y2)
    {
        ctx.PendingActions.Enqueue(snapshot =>
        {
            var line = snapshot.Lines.FirstOrDefault();
            if (line is null) return PlayerAction.None;
            var from = snapshot.Stations[new Location(x1, y1)];
            var to = snapshot.Stations[new Location(x2, y2)];
            return new CreateLine(line.LineId, from.Id, to.Id);
        });
    }

    [Given(@"the runner will attempt to extend the first line from \((\d+),(\d+)\) back to \((\d+),(\d+)\)")]
    public void GivenAttemptExtendToExistingStation(int x1, int y1, int x2, int y2)
    {
        ctx.PendingActions.Enqueue(snapshot =>
        {
            var line = snapshot.Lines.FirstOrDefault();
            if (line is null) return PlayerAction.None;
            var from = snapshot.Stations[new Location(x1, y1)];
            var to = snapshot.Stations[new Location(x2, y2)];
            return new CreateLine(line.LineId, from.Id, to.Id);
        });
    }

    // ═══════════════════════════════════════════════════════════════════
    // AddVehicleToLine error-triggering steps
    // ═══════════════════════════════════════════════════════════════════

    [Given(@"the runner will attempt to deploy a train with a non-existent resource on the first line at station \((\d+),(\d+)\)")]
    public void GivenAttemptDeployTrainNonExistentResource(int x, int y)
    {
        ctx.PendingActions.Enqueue(snapshot =>
        {
            var line = snapshot.Lines.FirstOrDefault();
            if (line is null) return PlayerAction.None;
            var station = snapshot.Stations[new Location(x, y)];
            return new AddVehicleToLine(Guid.NewGuid(), line.LineId, station.Id);
        });
    }

    [Given(@"the runner will attempt to deploy a train on a non-existent line at station \((\d+),(\d+)\)")]
    public void GivenAttemptDeployTrainNonExistentLine(int x, int y)
    {
        ctx.PendingActions.Enqueue(snapshot =>
        {
            var train = snapshot.Resources.FirstOrDefault(r => r.Type == ResourceType.Train && !r.InUse);
            if (train is null) return PlayerAction.None;
            var station = snapshot.Stations[new Location(x, y)];
            return new AddVehicleToLine(train.Id, Guid.NewGuid(), station.Id);
        });
    }

    [Given(@"the runner will attempt to deploy a train at station \((\d+),(\d+)\) which is not on the first line")]
    public void GivenAttemptDeployTrainStationNotOnLine(int x, int y)
    {
        ctx.PendingActions.Enqueue(snapshot =>
        {
            var train = snapshot.Resources.FirstOrDefault(r => r.Type == ResourceType.Train && !r.InUse);
            var line = snapshot.Lines.FirstOrDefault();
            if (train is null || line is null) return PlayerAction.None;
            var station = snapshot.Stations[new Location(x, y)];
            return new AddVehicleToLine(train.Id, line.LineId, station.Id);
        });
    }

    // ═══════════════════════════════════════════════════════════════════
    // RemoveLine / RemoveVehicle steps
    // ═══════════════════════════════════════════════════════════════════

    [Given(@"the runner will attempt to remove the first line")]
    public void GivenAttemptRemoveLine()
    {
        ctx.PendingActions.Enqueue(snapshot =>
        {
            var line = snapshot.Lines.FirstOrDefault();
            return line is not null ? new RemoveLine(line.LineId) : new RemoveLine(Guid.NewGuid());
        });
    }

    [Given(@"the runner will attempt to remove the first train")]
    public void GivenAttemptRemoveVehicle()
    {
        ctx.PendingActions.Enqueue(snapshot =>
        {
            var train = snapshot.Trains.FirstOrDefault();
            return train is not null ? new RemoveVehicle(train.TrainId) : new RemoveVehicle(Guid.NewGuid());
        });
    }

    [Given(@"the runner will attempt to remove a non-existent train")]
    public void GivenAttemptRemoveNonExistentVehicle()
    {
        ctx.PendingActions.Enqueue(_ => new RemoveVehicle(Guid.NewGuid()));
    }
}
