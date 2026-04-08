using MetroMania.Domain.Enums;
using MetroMania.Engine.Contracts;
using MetroMania.Engine.Model;

namespace MetroMania.Demo.Runners;

/// <summary>
/// Demonstrates what happens to a train when a station is inserted into the
/// middle of its line and the new station is physically above the original
/// straight path (Level 10).
///
/// Timeline:
///   Phase 0 — CreateLine:         A ● (2,5) ────── C ■ (14,5)
///   Phase 1 — AddVehicleToLine:   deploy train at A
///   Phase 2-7 — NoAction:         let the train travel along the flat A→C segment
///   Phase 8 — ExtendLineInBetween: insert B ▲ (8,2) between A and C
///             The line path now bends up through B. The engine snaps the
///             train to the closest tile on the new A→B→C path.
///   Phase 6+ — NoAction:          watch the train travel the new bent route
/// </summary>
internal class MiddleInsertUpDemoRunner : IMetroManiaRunner
{
    private Guid? _circleId;     // A
    private Guid? _triangleId;   // B (above center)
    private Guid? _rectangleId;  // C

    private Guid? _lineId;
    private int _setupPhase;

    public PlayerAction OnHourTicked(GameSnapshot snapshot)
    {
        if (_circleId is null || _triangleId is null || _rectangleId is null)
            return new NoAction();

        var availableLines  = snapshot.Resources.Where(r => r.Type == ResourceType.Line  && !r.InUse).ToList();
        var availableTrains = snapshot.Resources.Where(r => r.Type == ResourceType.Train && !r.InUse).ToList();

        switch (_setupPhase)
        {
            case 0:
            {
                if (availableLines.Count < 1) return new NoAction();
                _setupPhase++;
                return new CreateLine(availableLines[0].Id, _circleId.Value, _rectangleId.Value);
            }

            case 1:
            {
                var line = snapshot.Lines.FirstOrDefault(l =>
                    l.StationIds.Contains(_circleId.Value) && l.StationIds.Contains(_rectangleId.Value));
                if (line is null || availableTrains.Count < 1) return new NoAction();
                _lineId = line.LineId;
                _setupPhase++;
                return new AddVehicleToLine(availableTrains[0].Id, line.LineId, _circleId.Value);
            }

            case >= 2 and <= 7:
            {
                _setupPhase++;
                return new NoAction();
            }

            case 8:
            {
                if (_lineId is null) return new NoAction();
                _setupPhase++;
                return new ExtendLineInBetween(_lineId.Value, _circleId.Value, _triangleId.Value, _rectangleId.Value);
            }

            default:
                return new NoAction();
        }
    }

    public void OnStationSpawned(GameSnapshot snapshot, Guid stationId, Location location, StationType stationType)
    {
        switch (stationType)
        {
            case StationType.Circle:    _circleId    = stationId; break;
            case StationType.Triangle:  _triangleId  = stationId; break;
            case StationType.Rectangle: _rectangleId = stationId; break;
        }
    }

    public void OnDayStart(GameSnapshot snapshot) { }
    public void OnWeeklyGiftReceived(GameSnapshot snapshot, ResourceType gift) { }
    public void OnPassengerSpawned(GameSnapshot snapshot, Guid stationId, Guid passengerId) { }
    public void OnStationCrowded(GameSnapshot snapshot, Guid stationId, int numberOfPassengersWaiting) { }
    public void OnGameOver(GameSnapshot snapshot, Guid stationId) { }

    public void OnInvalidPlayerAction(GameSnapshot snapshot, int code, string description) =>
        Console.WriteLine($"[Level10] Invalid action (code {code}): {description}");

    public void OnVehicleRemoved(GameSnapshot snapshot, Guid vehicleId) { }
    public void OnLineRemoved(GameSnapshot snapshot, Guid lineId) { }
}
