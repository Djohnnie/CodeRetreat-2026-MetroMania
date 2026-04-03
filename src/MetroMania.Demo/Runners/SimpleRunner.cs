using MetroMania.Domain.Enums;
using MetroMania.Engine.Contracts;
using MetroMania.Engine.Model;

namespace MetroMania.Demo.Runners;

internal class SimpleRunner : IMetroManiaRunner
{
    public PlayerAction OnHourTicked(GameSnapshot snapshot)
    {
        var availableLines  = snapshot.Resources.Where(r => r.Type == ResourceType.Line  && !r.InUse).ToList();
        var availableTrains = snapshot.Resources.Where(r => r.Type == ResourceType.Train && !r.InUse).ToList();
        var stations        = snapshot.Stations.Values.ToList();

        // Create the first line between the first two spawned stations
        if (snapshot.Lines.Count == 0 && availableLines.Count > 0 && stations.Count >= 2)
            return new CreateLine(availableLines[0].Id, stations[0].Id, stations[1].Id);

        // Extend the first line to any station not yet connected
        if (snapshot.Lines.Count > 0)
        {
            var line = snapshot.Lines[0];
            var connectedIds = line.StationIds.ToHashSet();
            var unconnected = stations.FirstOrDefault(s => !connectedIds.Contains(s.Id));
            if (unconnected is not null)
                return new CreateLine(line.LineId, line.StationIds[^1], unconnected.Id);
        }

        // Deploy a train once a line exists
        // (AddVehicleToLine requires Line.LineId — revisit when the Line model is extended)
        _ = availableTrains;

        return new NoAction();
    }

    public void OnDayStart(GameSnapshot snapshot) { }

    public void OnWeeklyGiftReceived(GameSnapshot snapshot, ResourceType gift) { }

    public void OnStationSpawned(GameSnapshot snapshot, Guid stationId, Location location, StationType stationType) { }

    public void OnPassengerSpawned(GameSnapshot snapshot, Guid stationId, Guid passengerId) { }

    public void OnStationOverrun(GameSnapshot snapshot, Guid stationId, int numberOfPassengersWaiting) { }

    public void OnGameOver(GameSnapshot snapshot, Guid stationId) { }
}
