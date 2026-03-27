using MetroMania.Engine.Model;
using MetroMania.Engine.Tests.Support;
using Reqnroll;

namespace MetroMania.Engine.Tests.StepDefinitions;

[Binding]
public class VehicleMovementStepDefinitions(EngineTestContext ctx)
{
    [Then(@"the vehicle should have segment index (\d+) with progress ([\d.]+) and direction (-?\d+)")]
    public void ThenTheVehicleShouldHaveSegmentIndexProgressAndDirection(int segmentIndex, float progress, int direction)
    {
        var vehicle = Assert.Single(ctx.Snapshot!.Vehicles);
        Assert.Equal(segmentIndex, vehicle.SegmentIndex);
        Assert.Equal(progress, vehicle.Progress, 0.01f);
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
}
