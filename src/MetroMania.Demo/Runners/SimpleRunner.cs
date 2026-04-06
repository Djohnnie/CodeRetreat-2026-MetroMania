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

        // Connect any unconnected station.
        // When a spare line resource is available, start a NEW line anchored to line0's HEAD
        // (index 0). This deliberately leaves line0's TAIL free so that on the very next tick
        // we will also extend line0 from its tail to the same station — giving it interchange
        // connectivity on both lines.
        //
        // Example with stations A, B, C:
        //   Line0: A–B  (head=A, tail=B)
        //   C spawns, resource available  →  new Line2: A–C  (takes the A–C segment)
        //   Next tick: extend Line0 tail B→C  →  Line0: A–B–C  ✅
        //
        // If no spare resource, extend line0 from its tail directly.
        if (snapshot.Lines.Count > 0)
        {
            var line = snapshot.Lines[0];
            var connectedIds = line.StationIds.ToHashSet();
            var unconnected = stations.FirstOrDefault(s => !connectedIds.Contains(s.Id));
            if (unconnected is not null)
            {
                if (availableLines.Count > 0)
                    // Anchor the new line at line0's head, not its tail.
                    return new CreateLine(availableLines[0].Id, line.StationIds[0], unconnected.Id);
                else
                    // No spare line — extend line0 from its tail.
                    return new ExtendLineFromTerminal(line.LineId, line.StationIds[^1], unconnected.Id);
            }
        }

        // Deploy a train onto a line that has no train yet, falling back to any line.
        if (availableTrains.Count > 0)
        {
            var trainlesLine = snapshot.Lines.FirstOrDefault(l =>
                l.StationIds.Count > 0 &&
                !snapshot.Trains.Any(t => t.LineId == l.LineId));
            var targetLine = trainlesLine ?? snapshot.Lines.FirstOrDefault(l => l.StationIds.Count > 0);
            if (targetLine is not null)
                return new AddVehicleToLine(availableTrains[0].Id, targetLine.LineId, targetLine.StationIds[0]);
        }

        return new NoAction();
    }

    public void OnDayStart(GameSnapshot snapshot) { }

    public void OnWeeklyGiftReceived(GameSnapshot snapshot, ResourceType gift) { }

    public void OnStationSpawned(GameSnapshot snapshot, Guid stationId, Location location, StationType stationType) { }

    public void OnPassengerSpawned(GameSnapshot snapshot, Guid stationId, Guid passengerId) { }

    public void OnStationCrowded(GameSnapshot snapshot, Guid stationId, int numberOfPassengersWaiting) { }

    public void OnGameOver(GameSnapshot snapshot, Guid stationId) { }

    public void OnInvalidPlayerAction(GameSnapshot snapshot, int code, string description) { Console.WriteLine(description); }

    public void OnVehicleRemoved(GameSnapshot snapshot, Guid vehicleId) { }
    public void OnLineRemoved(GameSnapshot snapshot, Guid lineId) { }
}
