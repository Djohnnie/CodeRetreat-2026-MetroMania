using MetroMania.Domain.Enums;
using MetroMania.Engine.Model;
using MetroMania.Engine.Tests.Support;
using Reqnroll;

namespace MetroMania.Engine.Tests.StepDefinitions;

[Binding]
public class PassengerSpawningStepDefinitions(EngineTestContext ctx)
{
    [Then(@"the last snapshot should contain (\d+) passengers?")]
    public void ThenSnapshotContainsNPassengers(int expected)
    {
        Assert.NotNull(ctx.LastSnapshot);
        Assert.Equal(expected, ctx.LastSnapshot.Passengers.Count);
    }

    [Then(@"all spawned passengers should have unique IDs")]
    public void ThenAllPassengersHaveUniqueIds()
    {
        var ids = ctx.PassengerSpawnedCalls.Select(e => e.PassengerId).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Then(@"all passenger spawn events should reference the station at \((\d+),(\d+)\)")]
    public void ThenAllPassengersReferenceStationAt(int x, int y)
    {
        Assert.NotEmpty(ctx.PassengerSpawnedCalls);
        var expectedStationId = ctx.StationIdsByLocation[new Location(x, y)];
        Assert.All(ctx.PassengerSpawnedCalls,
            e => Assert.Equal(expectedStationId, e.StationId));
    }

    [Then(@"all passenger spawn events should reference the (\w+) station at \((\d+),(\d+)\)")]
    public void ThenAllPassengersReferenceNamedStationAt(string type, int x, int y)
    {
        Assert.NotEmpty(ctx.PassengerSpawnedCalls);
        var expectedStationId = ctx.StationIdsByLocation[new Location(x, y)];
        Assert.All(ctx.PassengerSpawnedCalls,
            e => Assert.Equal(expectedStationId, e.StationId));
    }

    [Then(@"all spawned passengers should have a destination type different from (\w+)")]
    public void ThenAllPassengersHaveDifferentDestinationType(string stationTypeName)
    {
        var disallowed = Enum.Parse<StationType>(stationTypeName);
        Assert.NotEmpty(ctx.PassengerSpawnedCalls);
        Assert.All(ctx.PassengerSpawnedCalls,
            e => Assert.NotEqual(disallowed, e.DestinationType));
    }

    [Then(@"all spawned passengers should have a destination type different from the type of their origin station")]
    public void ThenAllPassengersHaveDifferentDestinationFromOrigin()
    {
        Assert.NotEmpty(ctx.PassengerSpawnedCalls);
        foreach (var ev in ctx.PassengerSpawnedCalls)
        {
            var stationEntry = ctx.LastSnapshot!.Stations.FirstOrDefault(s => s.Value.Id == ev.StationId);
            Assert.NotEqual(Guid.Empty, stationEntry.Value?.Id ?? Guid.Empty);
            Assert.NotEqual(stationEntry.Value!.StationType, ev.DestinationType);
        }
    }

    [Then(@"passenger spawn events should reference both stations")]
    public void ThenPassengerEventsReferenceBothStations()
    {
        Assert.NotEmpty(ctx.PassengerSpawnedCalls);
        var referencedIds = ctx.PassengerSpawnedCalls.Select(e => e.StationId).Distinct().ToHashSet();
        Assert.True(referencedIds.Count >= 2,
            $"Expected passenger events to reference at least 2 distinct stations, but got {referencedIds.Count}");
    }

    [Then(@"no passenger spawn events should reference the station at \((\d+),(\d+)\)")]
    public void ThenNoPassengerEventsReferenceStationAt(int x, int y)
    {
        if (!ctx.StationIdsByLocation.TryGetValue(new Location(x, y), out var stationId))
            return; // station never spawned — trivially satisfied

        Assert.DoesNotContain(ctx.PassengerSpawnedCalls, e => e.StationId == stationId);
    }
}
