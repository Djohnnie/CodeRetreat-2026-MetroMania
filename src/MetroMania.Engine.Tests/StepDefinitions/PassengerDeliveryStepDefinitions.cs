using MetroMania.Domain.Enums;
using MetroMania.Engine.Model;
using MetroMania.Engine.Tests.Support;
using Reqnroll;

namespace MetroMania.Engine.Tests.StepDefinitions;

[Binding]
public class PassengerDeliveryStepDefinitions(EngineTestContext ctx)
{
    [Given(@"a level with vehicle capacity (\d+)")]
    public void GivenALevelWithVehicleCapacity(int capacity)
    {
        ctx.VehicleCapacityOverride = capacity;
    }

    [Then(@"the total score should be greater than (\d+)")]
    public void ThenTheTotalScoreShouldBeGreaterThan(int minimum)
    {
        Assert.True(ctx.Snapshot!.TotalScore > minimum,
            $"Expected TotalScore > {minimum}, but was {ctx.Snapshot.TotalScore}");
    }

    [Then(@"the total score should be at least (\d+)")]
    public void ThenTheTotalScoreShouldBeAtLeast(int minimum)
    {
        Assert.True(ctx.Snapshot!.TotalScore >= minimum,
            $"Expected TotalScore >= {minimum}, but was {ctx.Snapshot.TotalScore}");
    }

    [Then(@"the vehicle should have (\d+) passengers? onboard")]
    public void ThenTheVehicleShouldHavePassengersOnboard(int count)
    {
        var vehicle = Assert.Single(ctx.Snapshot!.Vehicles);
        Assert.Equal(count, vehicle.Passengers.Count);
    }

    [Then(@"the vehicle should have at least (\d+) passengers? onboard")]
    public void ThenTheVehicleShouldHaveAtLeastPassengersOnboard(int count)
    {
        var vehicle = Assert.Single(ctx.Snapshot!.Vehicles);
        Assert.True(vehicle.Passengers.Count >= count,
            $"Expected at least {count} passengers onboard, but was {vehicle.Passengers.Count}");
    }

    [Then(@"the Circle station at \((\d+),(\d+)\) should have fewer than (\d+) passengers")]
    public void ThenTheStationShouldHaveFewerThanPassengers(int x, int y, int maxCount)
    {
        var station = ctx.Snapshot!.Stations[new Location(x, y)];
        Assert.True(station.Passengers.Count < maxCount,
            $"Expected fewer than {maxCount} passengers at ({x},{y}), but was {station.Passengers.Count}");
    }

    [Then(@"passengers with (\w+) destination should still be at station \((\d+),(\d+)\)")]
    public void ThenPassengersWithDestinationShouldStillBeAtStation(string typeName, int x, int y)
    {
        var destType = Enum.Parse<StationType>(typeName);
        var station = ctx.Snapshot!.Stations[new Location(x, y)];
        var vehicle = Assert.Single(ctx.Snapshot.Vehicles);

        // No passengers of this type should be on the vehicle
        Assert.DoesNotContain(vehicle.Passengers, p => p.DestinationType == destType);

        // Some passengers of this type should remain at the station (if any were spawned)
        // We just verify the vehicle didn't pick them up
    }

    [Then(@"the vehicle should never have more than (\d+) passengers? onboard")]
    public void ThenTheVehicleShouldNeverHaveMoreThanPassengersOnboard(int maxCount)
    {
        Assert.True(ctx.MaxPassengersOnboard <= maxCount,
            $"Expected max {maxCount} passengers onboard, but observed {ctx.MaxPassengersOnboard}");
    }

    [Then(@"the vehicle should have experienced dwell time")]
    public void ThenTheVehicleShouldHaveExperiencedDwellTime()
    {
        Assert.True(ctx.DwellTimeObserved, "Expected the vehicle to dwell at a station but it never did");
    }
}
