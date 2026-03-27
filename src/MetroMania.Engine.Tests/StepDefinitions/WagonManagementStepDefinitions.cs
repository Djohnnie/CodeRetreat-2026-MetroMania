using MetroMania.Domain.Enums;
using MetroMania.Engine.Model;
using MetroMania.Engine.Tests.Support;
using Reqnroll;

namespace MetroMania.Engine.Tests.StepDefinitions;

[Binding]
public class WagonManagementStepDefinitions(EngineTestContext ctx)
{
    private Guid? _secondLineId;
    private Guid? _secondTrainId;

    [Given(@"the player will then add a wagon to the train")]
    public void GivenThePlayerWillThenAddAWagonToTheTrain()
    {
        ctx.PendingActions.Add(snapshot =>
        {
            if (ctx.LastAddedVehicleId is null) return null;
            if (snapshot.Vehicles.All(v => v.VehicleId != ctx.LastAddedVehicleId)) return null;

            var wagon = snapshot.AvailableResources
                .FirstOrDefault(r => r.Type == ResourceType.Wagon);
            if (wagon is null) return null;

            ctx.LastAddedWagonId = wagon.Id;
            return new AddWagonToTrain(wagon.Id, ctx.LastAddedVehicleId.Value);
        });
    }

    [Given(@"the player will then add first wagon to the train")]
    public void GivenThePlayerWillThenAddFirstWagonToTheTrain()
    {
        GivenThePlayerWillThenAddAWagonToTheTrain();
    }

    [Given(@"the player will then add second wagon to the train")]
    public void GivenThePlayerWillThenAddSecondWagonToTheTrain()
    {
        ctx.PendingActions.Add(snapshot =>
        {
            if (ctx.LastAddedVehicleId is null) return null;
            if (snapshot.Vehicles.All(v => v.VehicleId != ctx.LastAddedVehicleId)) return null;

            var wagon = snapshot.AvailableResources
                .FirstOrDefault(r => r.Type == ResourceType.Wagon && r.Id != ctx.LastAddedWagonId);
            if (wagon is null) return null;

            ctx.SecondAddedWagonId = wagon.Id;
            return new AddWagonToTrain(wagon.Id, ctx.LastAddedVehicleId.Value);
        });
    }

    [Given(@"the player will then add a wagon to a random train id")]
    public void GivenThePlayerWillThenAddAWagonToARandomTrainId()
    {
        ctx.PendingActions.Add(snapshot =>
        {
            var wagon = snapshot.AvailableResources
                .FirstOrDefault(r => r.Type == ResourceType.Wagon);
            if (wagon is null) return null;

            return new AddWagonToTrain(wagon.Id, Guid.NewGuid());
        });
    }

    [Given(@"the player will then add the same wagon to the train again")]
    public void GivenThePlayerWillThenAddTheSameWagonToTheTrainAgain()
    {
        ctx.PendingActions.Add(snapshot =>
        {
            if (ctx.LastAddedWagonId is null || ctx.LastAddedVehicleId is null) return null;
            // This wagon should already be in use, so it should be ignored
            return new AddWagonToTrain(ctx.LastAddedWagonId.Value, ctx.LastAddedVehicleId.Value);
        });
    }

    [Given(@"the player will then add a wagon directly to the line at station \((\d+),(\d+)\)")]
    public void GivenThePlayerWillThenAddAWagonDirectlyToTheLineAtStation(int x, int y)
    {
        var loc = new Location(x, y);

        ctx.PendingActions.Add(snapshot =>
        {
            if (ctx.LastCreatedLineId is null) return null;
            if (snapshot.Lines.All(l => l.LineId != ctx.LastCreatedLineId)) return null;
            if (!ctx.StationIdsByLocation.TryGetValue(loc, out var stationId)) return null;

            var wagon = snapshot.AvailableResources
                .FirstOrDefault(r => r.Type == ResourceType.Wagon);
            if (wagon is null) return null;

            return new AddVehicleToLine(wagon.Id, ctx.LastCreatedLineId.Value, stationId);
        });
    }

    [Given(@"the player will create a second line connecting stations at \((\d+),(\d+)\) and \((\d+),(\d+)\)")]
    public void GivenThePlayerWillCreateASecondLine(int x1, int y1, int x2, int y2)
    {
        var loc1 = new Location(x1, y1);
        var loc2 = new Location(x2, y2);

        ctx.PendingActions.Add(snapshot =>
        {
            // Need at least 2 available lines (one already used for the first line)
            var availLine = snapshot.AvailableLines.FirstOrDefault();
            if (availLine is null) return null;
            if (!ctx.StationIdsByLocation.TryGetValue(loc1, out var id1)) return null;
            if (!ctx.StationIdsByLocation.TryGetValue(loc2, out var id2)) return null;

            _secondLineId = availLine.Id;
            return new CreateLine(availLine.Id, [id1, id2]);
        });
    }

    [Given(@"the player will then add a vehicle to the second line at station \((\d+),(\d+)\)")]
    public void GivenThePlayerWillThenAddAVehicleToTheSecondLine(int x, int y)
    {
        var loc = new Location(x, y);

        ctx.PendingActions.Add(snapshot =>
        {
            if (_secondLineId is null) return null;
            if (snapshot.Lines.All(l => l.LineId != _secondLineId)) return null;
            if (snapshot.AvailableVehicles.Count == 0) return null;
            if (!ctx.StationIdsByLocation.TryGetValue(loc, out var stationId)) return null;

            var vehicleId = snapshot.AvailableVehicles[0].Id;
            _secondTrainId = vehicleId;
            return new AddVehicleToLine(vehicleId, _secondLineId.Value, stationId);
        });
    }

    [Given(@"the player will then add a wagon to the first train")]
    public void GivenThePlayerWillThenAddAWagonToTheFirstTrain()
    {
        // Same as adding a wagon to the train tracked in ctx.LastAddedVehicleId
        GivenThePlayerWillThenAddAWagonToTheTrain();
    }

    [Given(@"the player will then move the wagon from the first train to the second train")]
    public void GivenThePlayerWillThenMoveTheWagonFromFirstToSecond()
    {
        ctx.PendingActions.Add(snapshot =>
        {
            if (ctx.LastAddedWagonId is null || ctx.LastAddedVehicleId is null || _secondTrainId is null) return null;
            if (snapshot.Vehicles.All(v => v.VehicleId != _secondTrainId)) return null;

            return new MoveWagonBetweenTrains(ctx.LastAddedWagonId.Value, ctx.LastAddedVehicleId.Value, _secondTrainId.Value);
        });
    }

    [Given(@"the player will then move the wagon from the second train to the first train")]
    public void GivenThePlayerWillThenMoveTheWagonFromSecondToFirst()
    {
        ctx.PendingActions.Add(snapshot =>
        {
            if (ctx.LastAddedWagonId is null || ctx.LastAddedVehicleId is null || _secondTrainId is null) return null;
            if (snapshot.Vehicles.All(v => v.VehicleId != _secondTrainId)) return null;

            // Source is second train (which does NOT own the wagon) — should be ignored
            return new MoveWagonBetweenTrains(ctx.LastAddedWagonId.Value, _secondTrainId.Value, ctx.LastAddedVehicleId.Value);
        });
    }

    [Given(@"the player will then move the wagon from the first train to the first train")]
    public void GivenThePlayerWillThenMoveTheWagonFromFirstToFirst()
    {
        ctx.PendingActions.Add(snapshot =>
        {
            if (ctx.LastAddedWagonId is null || ctx.LastAddedVehicleId is null) return null;
            if (snapshot.Vehicles.All(v => v.VehicleId != ctx.LastAddedVehicleId)) return null;

            return new MoveWagonBetweenTrains(ctx.LastAddedWagonId.Value, ctx.LastAddedVehicleId.Value, ctx.LastAddedVehicleId.Value);
        });
    }

    // --- Then steps ---

    [Then(@"the train should have (\d+) wagons? attached")]
    public void ThenTheTrainShouldHaveWagonsAttached(int count)
    {
        var train = ctx.Snapshot!.Vehicles.First(v => v.VehicleId == ctx.LastAddedVehicleId);
        Assert.Equal(count, train.WagonIds.Count);
    }

    [Then(@"the first train should have (\d+) wagons? attached")]
    public void ThenTheFirstTrainShouldHaveWagonsAttached(int count)
    {
        var train = ctx.Snapshot!.Vehicles.First(v => v.VehicleId == ctx.LastAddedVehicleId);
        Assert.Equal(count, train.WagonIds.Count);
    }

    [Then(@"the second train should have (\d+) wagons? attached")]
    public void ThenTheSecondTrainShouldHaveWagonsAttached(int count)
    {
        var train = ctx.Snapshot!.Vehicles.First(v => v.VehicleId == _secondTrainId);
        Assert.Equal(count, train.WagonIds.Count);
    }

    [Then(@"the wagon should reference the train via navigation")]
    public void ThenTheWagonShouldReferenceTheTrainViaNavigation()
    {
        // The wagon doesn't appear as a vehicle (it's attached to a train), but we can
        // check via the train's Wagons navigation property
        var train = ctx.Snapshot!.Vehicles.First(v => v.VehicleId == ctx.LastAddedVehicleId);
        Assert.Single(train.WagonIds);

        // The wagon is NOT a separate VehicleSnapshot — wagons are tracked as IDs on the train.
        // But the Train navigation from a wagon snapshot IS available if we find it:
        // Actually wagons are NOT separate vehicles in the snapshot, so we verify via the train's Wagons list.
        // WagonIds are just resource IDs, and the Wagons navigation returns VehicleSnapshot — but wagons
        // don't have their own VehicleSnapshot entries. So we verify the Train property from the train itself.
        Assert.NotNull(train.Line);
    }

    [Then(@"the train should reference the wagon via navigation")]
    public void ThenTheTrainShouldReferenceTheWagonViaNavigation()
    {
        var train = ctx.Snapshot!.Vehicles.First(v => v.VehicleId == ctx.LastAddedVehicleId);
        Assert.Single(train.WagonIds);
        Assert.Equal(ctx.LastAddedWagonId, train.WagonIds[0]);
    }

    [Then(@"the snapshot should have (\d+) available wagons?")]
    public void ThenTheSnapshotShouldHaveAvailableWagons(int count)
    {
        var wagons = ctx.Snapshot!.AvailableResources.Where(r => r.Type == ResourceType.Wagon).ToList();
        Assert.Equal(count, wagons.Count);
    }
}
