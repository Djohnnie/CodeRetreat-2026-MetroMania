using MetroMania.Engine.Model;
using MetroMania.Engine.Tests.Support;
using Reqnroll;

namespace MetroMania.Engine.Tests.StepDefinitions;

[Binding]
public class VehicleMovementStepDefinitions(EngineTestContext ctx)
{
    [Given(@"the player will then add a second vehicle to the created line at station \((\d+),(\d+)\)")]
    public void GivenThePlayerWillThenAddASecondVehicle(int x, int y)
    {
        var loc = new Location(x, y);

        ctx.PendingActions.Add(snapshot =>
        {
            if (ctx.LastCreatedLineId is null) return null;
            // Need a vehicle resource that is NOT the first one
            var available = snapshot.AvailableVehicles
                .Where(r => r.Id != ctx.LastAddedVehicleId)
                .FirstOrDefault();
            if (available is null) return null;
            if (snapshot.Lines.All(l => l.LineId != ctx.LastCreatedLineId)) return null;
            if (!ctx.StationIdsByLocation.TryGetValue(loc, out var stationId)) return null;

            ctx.SecondAddedVehicleId = available.Id;
            return new AddVehicleToLine(available.Id, ctx.LastCreatedLineId.Value, stationId);
        });
    }

    [Then(@"the vehicle should have segment index (\d+) with progress ([\d.]+) and direction (-?\d+)")]
    public void ThenTheVehicleShouldHaveSegmentIndexProgressAndDirection(int segmentIndex, decimal progress, int direction)
    {
        var vehicle = Assert.Single(ctx.Snapshot!.Vehicles);
        Assert.Equal(segmentIndex, vehicle.SegmentIndex);
        Assert.Equal(progress, vehicle.Progress);
        Assert.Equal(direction, vehicle.Direction);
    }

    [Then(@"the vehicle should not be at a station")]
    public void ThenTheVehicleShouldNotBeAtAStation()
    {
        var vehicle = Assert.Single(ctx.Snapshot!.Vehicles);
        Assert.Null(vehicle.StationId);
    }

    [Then(@"the vehicle should be at station \((\d+),(\d+)\) with direction (-?\d+)")]
    public void ThenTheVehicleShouldBeAtStationWithDirection(int x, int y, int direction)
    {
        var vehicle = Assert.Single(ctx.Snapshot!.Vehicles);
        Assert.NotNull(vehicle.StationId);
        Assert.Equal(ctx.StationIdsByLocation[new Location(x, y)], vehicle.StationId.Value);
        Assert.Equal(direction, vehicle.Direction);
    }

    [Then(@"vehicle (\d+) should be at station \((\d+),(\d+)\) with direction (-?\d+)")]
    public void ThenVehicleNShouldBeAtStationWithDirection(int index, int x, int y, int direction)
    {
        var vehicle = ctx.Snapshot!.Vehicles[index];
        Assert.NotNull(vehicle.StationId);
        Assert.Equal(ctx.StationIdsByLocation[new Location(x, y)], vehicle.StationId.Value);
        Assert.Equal(direction, vehicle.Direction);
    }
}
