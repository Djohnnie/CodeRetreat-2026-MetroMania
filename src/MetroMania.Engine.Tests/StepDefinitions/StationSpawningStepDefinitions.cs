using MetroMania.Domain.Enums;
using MetroMania.Engine.Model;
using MetroMania.Engine.Tests.Support;
using Reqnroll;

namespace MetroMania.Engine.Tests.StepDefinitions;

[Binding]
public class StationSpawningStepDefinitions(EngineTestContext ctx)
{
    [Then(@"the (\w+) station at \((\d+),(\d+)\) should have spawned")]
    public void ThenStationShouldHaveSpawned(string type, int x, int y)
    {
        var stationType = Enum.Parse<StationType>(type);
        bool spawned = ctx.StationSpawnedCalls.Any(e => e.Location.X == x && e.Location.Y == y && e.StationType == stationType);
        Assert.True(spawned, $"Expected {type} station at ({x},{y}) to have spawned");
    }

    [Then(@"the (\w+) station at \((\d+),(\d+)\) should not have spawned")]
    public void ThenStationShouldNotHaveSpawned(string type, int x, int y)
    {
        var stationType = Enum.Parse<StationType>(type);
        bool spawned = ctx.StationSpawnedCalls.Any(e => e.Location.X == x && e.Location.Y == y && e.StationType == stationType);
        Assert.False(spawned, $"Expected {type} station at ({x},{y}) not to have spawned yet");
    }

    [Then(@"the game snapshot should contain a (\w+) station at \((\d+),(\d+)\)")]
    public void ThenSnapshotContainsStation(string type, int x, int y)
    {
        Assert.NotNull(ctx.LastSnapshot);
        var stationType = Enum.Parse<StationType>(type);
        var loc = new Location(x, y);
        Assert.True(ctx.LastSnapshot.Stations.TryGetValue(loc, out var station),
            $"Expected snapshot to contain a station at ({x},{y})");
        Assert.Equal(stationType, station.StationType);
    }

    [Then(@"the game snapshot should contain no stations")]
    public void ThenSnapshotContainsNoStations()
    {
        Assert.NotNull(ctx.LastSnapshot);
        Assert.Empty(ctx.LastSnapshot.Stations);
    }

    [Then(@"the game snapshot should contain (\d+) stations?")]
    public void ThenSnapshotContainsNStations(int expected)
    {
        Assert.NotNull(ctx.LastSnapshot);
        Assert.Equal(expected, ctx.LastSnapshot.Stations.Count);
    }

    [Then(@"all spawned stations should have unique IDs")]
    public void ThenSpawnedStationsHaveUniqueIds()
    {
        var ids = ctx.StationSpawnedCalls.Select(e => e.StationId).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Then(@"the spawned station should have been assigned a non-empty GUID")]
    public void ThenSpawnedStationHasNonEmptyGuid()
    {
        Assert.NotEmpty(ctx.StationSpawnedCalls);
        Assert.All(ctx.StationSpawnedCalls, e => Assert.NotEqual(Guid.Empty, e.StationId));
    }

    [Then(@"the (\w+) station at \((\d+),(\d+)\) should have spawned on day (\d+) at hour (\d+)")]
    public void ThenStationSpawnedOnDayAtHour(string type, int x, int y, int day, int hour)
    {
        var stationType = Enum.Parse<StationType>(type);
        var ev = ctx.StationSpawnedCalls.FirstOrDefault(e =>
            e.Location.X == x && e.Location.Y == y && e.StationType == stationType);
        Assert.NotNull(ev);
        Assert.Equal(day, ev.Time.Day);
        Assert.Equal(hour, ev.Time.Hour);
    }

    [Then(@"the (\w+) station at \((\d+),(\d+)\) should appear in the snapshot with the correct station type")]
    public void ThenStationInSnapshotHasCorrectType(string type, int x, int y)
    {
        Assert.NotNull(ctx.LastSnapshot);
        var stationType = Enum.Parse<StationType>(type);
        var loc = new Location(x, y);
        Assert.True(ctx.LastSnapshot.Stations.TryGetValue(loc, out var station));
        Assert.Equal(stationType, station.StationType);
    }

    [Then(@"each spawned station should appear in the game snapshot at its configured grid coordinates")]
    public void ThenAllSpawnedStationsInSnapshot()
    {
        Assert.NotNull(ctx.LastSnapshot);
        foreach (var ev in ctx.StationSpawnedCalls)
        {
            Assert.True(ctx.LastSnapshot.Stations.ContainsKey(ev.Location),
                $"Expected station at ({ev.Location.X},{ev.Location.Y}) to be in snapshot");
        }
    }
}
