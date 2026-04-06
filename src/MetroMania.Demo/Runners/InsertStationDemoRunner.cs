using MetroMania.Domain.Enums;
using MetroMania.Engine.Contracts;
using MetroMania.Engine.Model;

namespace MetroMania.Demo.Runners;

/// <summary>
/// A runner designed for Level 9 to demonstrate ExtendLineInBetween.
///
/// Network evolution:
///
///   Phase 1 — Create Line 1:   Circle(A) ──── Rectangle(C)
///   Phase 2 — Deploy Train 1 on Line 1 at Circle(A)
///   Phase 3 — (wait 3 ticks for the train to start travelling)
///   Phase 4 — Insert Triangle(B) into Line 1:   A ── B ── C
///   Phase 5 — Create Line 2:   Triangle(B) ── Star(D)
///   Phase 6 — Deploy Train 2 on Line 2 at Star(D)
///
/// The key moment is Phase 4: ExtendLineInBetween inserts Triangle between
/// Circle and Rectangle while Train 1 is already moving along that segment.
/// The engine recalculates Train 1's path index so it continues without issues.
/// </summary>
internal class InsertStationDemoRunner : IMetroManiaRunner
{
    private Guid? _circleId;
    private Guid? _rectangleId;
    private Guid? _triangleId;
    private Guid? _starId;

    private Guid? _line1Id;

    /// <summary>
    /// Setup phase counter — advances by one per successful action.
    /// 0 → CreateLine:              Circle → Rectangle  (Line 1)
    /// 1 → AddVehicleToLine:        Train 1 on Line 1 at Circle
    /// 2 → NoAction (wait tick 1)
    /// 3 → NoAction (wait tick 2)
    /// 4 → NoAction (wait tick 3)
    /// 5 → ExtendLineInBetween:     Insert Triangle between Circle and Rectangle
    /// 6 → CreateLine:              Triangle → Star  (Line 2)
    /// 7 → AddVehicleToLine:        Train 2 on Line 2 at Star
    /// 8+ → NoAction
    /// </summary>
    private int _setupPhase;

    public PlayerAction OnHourTicked(GameSnapshot snapshot)
    {
        if (_circleId is null || _rectangleId is null || _triangleId is null || _starId is null)
            return new NoAction();

        var availableLines  = snapshot.Resources.Where(r => r.Type == ResourceType.Line  && !r.InUse).ToList();
        var availableTrains = snapshot.Resources.Where(r => r.Type == ResourceType.Train && !r.InUse).ToList();

        switch (_setupPhase)
        {
            case 0:
            {
                // Create Line 1: Circle → Rectangle (skip Triangle for now)
                if (availableLines.Count < 1) return new NoAction();
                _setupPhase++;
                return new CreateLine(availableLines[0].Id, _circleId.Value, _rectangleId.Value);
            }

            case 1:
            {
                // Deploy Train 1 on Line 1 at Circle
                var line1 = snapshot.Lines.FirstOrDefault(l =>
                    l.StationIds.Contains(_circleId.Value) && l.StationIds.Contains(_rectangleId.Value));
                if (line1 is null || availableTrains.Count < 1) return new NoAction();
                _line1Id = line1.LineId;
                _setupPhase++;
                return new AddVehicleToLine(availableTrains[0].Id, line1.LineId, _circleId.Value);
            }

            case 2:
            case 3:
            case 4:
            {
                // Wait 3 ticks — let the train start moving along Line 1
                _setupPhase++;
                return new NoAction();
            }

            case 5:
            {
                // Insert Triangle between Circle and Rectangle on Line 1
                if (_line1Id is null) return new NoAction();
                _setupPhase++;
                return new ExtendLineInBetween(_line1Id.Value, _circleId.Value, _triangleId.Value, _rectangleId.Value);
            }

            case 6:
            {
                // Create Line 2: Triangle → Star
                if (availableLines.Count < 1) return new NoAction();
                _setupPhase++;
                return new CreateLine(availableLines[0].Id, _triangleId.Value, _starId.Value);
            }

            case 7:
            {
                // Deploy Train 2 on Line 2 at Star (avoid occupancy clash with Train 1 at Triangle)
                var line2 = snapshot.Lines.FirstOrDefault(l =>
                    l.StationIds.Contains(_triangleId.Value) &&
                    l.StationIds.Contains(_starId.Value) &&
                    !l.StationIds.Contains(_circleId.Value));
                if (line2 is null || availableTrains.Count < 1) return new NoAction();
                _setupPhase++;
                return new AddVehicleToLine(availableTrains[0].Id, line2.LineId, _starId.Value);
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
            case StationType.Star:      _starId      = stationId; break;
        }
    }

    public void OnDayStart(GameSnapshot snapshot) { }

    public void OnWeeklyGiftReceived(GameSnapshot snapshot, ResourceType gift) { }

    public void OnPassengerSpawned(GameSnapshot snapshot, Guid stationId, Guid passengerId) { }

    public void OnStationCrowded(GameSnapshot snapshot, Guid stationId, int numberOfPassengersWaiting) { }

    public void OnGameOver(GameSnapshot snapshot, Guid stationId) { }

    public void OnInvalidPlayerAction(GameSnapshot snapshot, int code, string description) =>
        Console.WriteLine($"[Level9] Invalid action (code {code}): {description}");

    public void OnVehicleRemoved(GameSnapshot snapshot, Guid vehicleId) { }
    public void OnLineRemoved(GameSnapshot snapshot, Guid lineId) { }
}
