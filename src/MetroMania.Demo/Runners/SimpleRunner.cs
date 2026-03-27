using MetroMania.Domain.Enums;
using MetroMania.Engine.Contracts;
using MetroMania.Engine.Model;

namespace MetroMania.Demo.Runners;

internal class SimpleRunner : IMetroManiaRunner
{
    public PlayerAction OnHourTick(GameSnapshot snapshot)
    {
        var lineWithoutVehicle = snapshot.Lines
            .FirstOrDefault(l => l.Vehicles.Count == 0);

        if (lineWithoutVehicle is not null && snapshot.AvailableVehicles.Count > 0)
        {
            var vehicleId = snapshot.AvailableVehicles[0].Id;
            var stationId = lineWithoutVehicle.Stations
                .MaxBy(s => s.Passengers.Count)!
                .Id;
            return new AddVehicleToLine(vehicleId, lineWithoutVehicle.LineId, stationId);
        }

        var unconnected = snapshot.UnconnectedStations;

        if (unconnected.Count == 0)
            return new NoAction();

        if (snapshot.Lines.Count > 0)
        {
            var line = snapshot.Lines[0];
            var fromStationId = line.StationIds[^1];
            var toStationId = unconnected[0].Id;
            return new ExtendLine(line.LineId, fromStationId, toStationId);
        }

        if (snapshot.AvailableLines.Count > 0 && unconnected.Count >= 2)
        {
            var lineId = snapshot.AvailableLines[0].Id;
            var stationIds = unconnected.Select(s => s.Id).ToList();
            return new CreateLine(lineId, stationIds);
        }

        return new NoAction();
    }

    public void OnDayStart(GameSnapshot snapshot) { }

    public void OnWeeklyGift(GameSnapshot snapshot, ResourceType gift) { }

    public void OnStationSpawned(GameSnapshot snapshot, Guid stationId, Location location, StationType stationType) { }

    public void OnPassengerWaiting(GameSnapshot snapshot, Location location, IReadOnlyList<Passenger> passengers) { }

    public void OnStationOverrun(GameSnapshot snapshot, Location location, IReadOnlyList<Passenger> passengers) { }

    public void OnGameOver(GameSnapshot snapshot, Location location, IReadOnlyList<Passenger> passengers) { }
}