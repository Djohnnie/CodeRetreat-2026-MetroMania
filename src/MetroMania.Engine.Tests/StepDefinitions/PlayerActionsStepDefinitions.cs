using MetroMania.Engine.Model;
using MetroMania.Engine.Tests.Support;
using Reqnroll;

namespace MetroMania.Engine.Tests.StepDefinitions;

[Binding]
public class PlayerActionsStepDefinitions(EngineTestContext ctx)
{
    [Given(@"the player will create a line connecting stations at \((\d+),(\d+)\) and \((\d+),(\d+)\)")]
    public void GivenThePlayerWillCreateALine(int x1, int y1, int x2, int y2)
    {
        var loc1 = new Location(x1, y1);
        var loc2 = new Location(x2, y2);

        ctx.PendingActions.Add(snapshot =>
        {
            if (snapshot.AvailableLines.Count == 0) return null;
            if (!ctx.StationIdsByLocation.TryGetValue(loc1, out var id1)) return null;
            if (!ctx.StationIdsByLocation.TryGetValue(loc2, out var id2)) return null;

            var lineId = snapshot.AvailableLines[0].Id;
            ctx.LastCreatedLineId = lineId;
            return new CreateLine(lineId, [id1, id2]);
        });
    }

    [Given(@"the player will then remove the created line")]
    public void GivenThePlayerWillThenRemoveTheCreatedLine()
    {
        ctx.PendingActions.Add(snapshot =>
        {
            if (ctx.LastCreatedLineId is null) return null;
            if (snapshot.Lines.All(l => l.LineId != ctx.LastCreatedLineId)) return null;
            return new RemoveLine(ctx.LastCreatedLineId.Value);
        });
    }

    [Given(@"the player will then add a vehicle to the created line at station \((\d+),(\d+)\)")]
    public void GivenThePlayerWillThenAddAVehicle(int x, int y)
    {
        var loc = new Location(x, y);

        ctx.PendingActions.Add(snapshot =>
        {
            if (ctx.LastCreatedLineId is null) return null;
            if (snapshot.AvailableVehicles.Count == 0) return null;
            if (snapshot.Lines.All(l => l.LineId != ctx.LastCreatedLineId)) return null;
            if (!ctx.StationIdsByLocation.TryGetValue(loc, out var stationId)) return null;

            var vehicleId = snapshot.AvailableVehicles[0].Id;
            ctx.LastAddedVehicleId = vehicleId;
            return new AddVehicleToLine(vehicleId, ctx.LastCreatedLineId.Value, stationId);
        });
    }

    [Given(@"the player will then remove the added vehicle")]
    public void GivenThePlayerWillThenRemoveTheAddedVehicle()
    {
        ctx.PendingActions.Add(snapshot =>
        {
            if (ctx.LastAddedVehicleId is null) return null;
            if (snapshot.Vehicles.All(v => v.VehicleId != ctx.LastAddedVehicleId)) return null;
            return new RemoveVehicle(ctx.LastAddedVehicleId.Value);
        });
    }

    [Given(@"the player will then extend the created line from station \((\d+),(\d+)\) to station \((\d+),(\d+)\)")]
    public void GivenThePlayerWillThenExtendTheCreatedLine(int fx, int fy, int tx, int ty)
    {
        var fromLoc = new Location(fx, fy);
        var toLoc = new Location(tx, ty);

        ctx.PendingActions.Add(snapshot =>
        {
            if (ctx.LastCreatedLineId is null) return null;
            if (snapshot.Lines.All(l => l.LineId != ctx.LastCreatedLineId)) return null;
            if (!ctx.StationIdsByLocation.TryGetValue(fromLoc, out var fromId)) return null;
            if (!ctx.StationIdsByLocation.TryGetValue(toLoc, out var toId)) return null;
            return new ExtendLine(ctx.LastCreatedLineId.Value, fromId, toId);
        });
    }

    [Given(@"the player will then insert station \((\d+),(\d+)\) between stations \((\d+),(\d+)\) and \((\d+),(\d+)\) on the created line")]
    public void GivenThePlayerWillThenInsertStation(int nx, int ny, int fx, int fy, int tx, int ty)
    {
        var newLoc = new Location(nx, ny);
        var fromLoc = new Location(fx, fy);
        var toLoc = new Location(tx, ty);

        ctx.PendingActions.Add(snapshot =>
        {
            if (ctx.LastCreatedLineId is null) return null;
            if (snapshot.Lines.All(l => l.LineId != ctx.LastCreatedLineId)) return null;
            if (!ctx.StationIdsByLocation.TryGetValue(newLoc, out var newId)) return null;
            if (!ctx.StationIdsByLocation.TryGetValue(fromLoc, out var fromId)) return null;
            if (!ctx.StationIdsByLocation.TryGetValue(toLoc, out var toId)) return null;
            return new InsertStationInLine(ctx.LastCreatedLineId.Value, newId, fromId, toId);
        });
    }

    [Given(@"the player will attempt to create a line with only station \((\d+),(\d+)\)")]
    public void GivenThePlayerWillAttemptToCreateALineWithOnlyOneStation(int x, int y)
    {
        var loc = new Location(x, y);

        ctx.PendingActions.Add(snapshot =>
        {
            if (snapshot.AvailableLines.Count == 0) return null;
            if (!ctx.StationIdsByLocation.TryGetValue(loc, out var id)) return null;

            var lineId = snapshot.AvailableLines[0].Id;
            return new CreateLine(lineId, [id]);
        });
    }

    [Given(@"the player will attempt to remove a line with a random id")]
    public void GivenThePlayerWillAttemptToRemoveALineWithARandomId()
    {
        ctx.PendingActions.Add(_ => new RemoveLine(Guid.NewGuid()));
    }

    // --- Then steps: connectivity ---

    [Then(@"the snapshot should have (\d+) connected stations?")]
    public void ThenTheSnapshotShouldHaveConnectedStations(int count)
    {
        Assert.Equal(count, ctx.Snapshot!.ConnectedStations.Count);
    }

    [Then(@"the snapshot should have (\d+) unconnected stations?")]
    public void ThenTheSnapshotShouldHaveUnconnectedStations(int count)
    {
        Assert.Equal(count, ctx.Snapshot!.UnconnectedStations.Count);
    }

    [Then(@"station \((\d+),(\d+)\) should be unconnected")]
    public void ThenStationShouldBeUnconnected(int x, int y)
    {
        var stationId = ctx.StationIdsByLocation[new Location(x, y)];
        Assert.Contains(ctx.Snapshot!.UnconnectedStations, s => s.Id == stationId);
    }

    [Then(@"station \((\d+),(\d+)\) should be connected")]
    public void ThenStationShouldBeConnected(int x, int y)
    {
        var stationId = ctx.StationIdsByLocation[new Location(x, y)];
        Assert.Contains(ctx.Snapshot!.ConnectedStations, s => s.Id == stationId);
    }

    // --- Then steps: navigation properties ---

    [Then(@"the active line should have (\d+) station snapshots?")]
    public void ThenTheActiveLineShouldHaveStationSnapshots(int count)
    {
        var line = Assert.Single(ctx.Snapshot!.Lines);
        Assert.Equal(count, line.Stations.Count);
    }

    [Then(@"station \((\d+),(\d+)\) should belong to (\d+) lines?")]
    public void ThenStationShouldBelongToLines(int x, int y, int count)
    {
        var stationId = ctx.StationIdsByLocation[new Location(x, y)];
        var station = ctx.Snapshot!.Stations.Values.Single(s => s.Id == stationId);
        Assert.Equal(count, station.Lines.Count);
    }

    [Then(@"the active line should have (\d+) vehicles?")]
    public void ThenTheActiveLineShouldHaveVehicles(int count)
    {
        var line = Assert.Single(ctx.Snapshot!.Lines);
        Assert.Equal(count, line.Vehicles.Count);
    }

    [Then(@"the active vehicle should reference the created line")]
    public void ThenTheActiveVehicleShouldReferenceTheCreatedLine()
    {
        var vehicle = Assert.Single(ctx.Snapshot!.Vehicles);
        Assert.NotNull(vehicle.Line);
        Assert.Equal(ctx.LastCreatedLineId, vehicle.Line.LineId);
    }

    [Then(@"the active vehicle should reference its resource")]
    public void ThenTheActiveVehicleShouldReferenceItsResource()
    {
        var vehicle = Assert.Single(ctx.Snapshot!.Vehicles);
        Assert.NotNull(vehicle.Resource);
        Assert.Equal(vehicle.VehicleId, vehicle.Resource.Id);
        Assert.True(vehicle.Resource.InUse);
    }

    [Then(@"the active line should reference its resource")]
    public void ThenTheActiveLineShouldReferenceItsResource()
    {
        var line = Assert.Single(ctx.Snapshot!.Lines);
        Assert.NotNull(line.Resource);
        Assert.Equal(line.LineId, line.Resource.Id);
        Assert.True(line.Resource.InUse);
    }

    [Then(@"the active vehicle at station \((\d+),(\d+)\) should reference the station snapshot")]
    public void ThenTheActiveVehicleAtStationShouldReferenceTheStationSnapshot(int x, int y)
    {
        var vehicle = Assert.Single(ctx.Snapshot!.Vehicles);
        Assert.NotNull(vehicle.Station);
        Assert.Equal(ctx.StationIdsByLocation[new Location(x, y)], vehicle.Station.Id);
    }

    // --- Then steps: lines/vehicles ---

    [Then(@"the snapshot should have (\d+) active lines?")]
    public void ThenTheSnapshotShouldHaveActiveLines(int count)
    {
        Assert.Equal(count, ctx.Snapshot!.Lines.Count);
    }

    [Then(@"the snapshot should have (\d+) active vehicles?")]
    public void ThenTheSnapshotShouldHaveActiveVehicles(int count)
    {
        Assert.Equal(count, ctx.Snapshot!.Vehicles.Count);
    }

    [Then(@"the snapshot should have (\d+) available lines?")]
    public void ThenTheSnapshotShouldHaveAvailableLines(int count)
    {
        Assert.Equal(count, ctx.Snapshot!.AvailableLines.Count);
    }

    [Then(@"the snapshot should have (\d+) available vehicles?")]
    public void ThenTheSnapshotShouldHaveAvailableVehicles(int count)
    {
        Assert.Equal(count, ctx.Snapshot!.AvailableVehicles.Count);
    }

    [Then(@"the active line should connect stations \((\d+),(\d+)\) and \((\d+),(\d+)\) in order")]
    public void ThenTheActiveLineShouldConnect2Stations(int x1, int y1, int x2, int y2)
    {
        var line = Assert.Single(ctx.Snapshot!.Lines);
        var expected = new[]
        {
            ctx.StationIdsByLocation[new Location(x1, y1)],
            ctx.StationIdsByLocation[new Location(x2, y2)]
        };
        Assert.Equal(expected, line.StationIds);
    }

    [Then(@"the active line should connect stations \((\d+),(\d+)\), \((\d+),(\d+)\) and \((\d+),(\d+)\) in order")]
    public void ThenTheActiveLineShouldConnect3Stations(int x1, int y1, int x2, int y2, int x3, int y3)
    {
        var line = Assert.Single(ctx.Snapshot!.Lines);
        var expected = new[]
        {
            ctx.StationIdsByLocation[new Location(x1, y1)],
            ctx.StationIdsByLocation[new Location(x2, y2)],
            ctx.StationIdsByLocation[new Location(x3, y3)]
        };
        Assert.Equal(expected, line.StationIds);
    }

    [Then(@"the active vehicle should be on the created line at station \((\d+),(\d+)\)")]
    public void ThenTheActiveVehicleShouldBeOnTheCreatedLineAtStation(int x, int y)
    {
        var vehicle = Assert.Single(ctx.Snapshot!.Vehicles);
        Assert.Equal(ctx.LastCreatedLineId, vehicle.LineId);
        Assert.NotNull(vehicle.StationId);
        Assert.Equal(ctx.StationIdsByLocation[new Location(x, y)], vehicle.StationId.Value);
    }
}
