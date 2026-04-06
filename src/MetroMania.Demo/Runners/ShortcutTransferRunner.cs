using MetroMania.Domain.Enums;
using MetroMania.Engine.Contracts;
using MetroMania.Engine.Model;

namespace MetroMania.Demo.Runners;

/// <summary>
/// Runner for Level 5 — proves that passengers prefer the shorter Line 2
/// shortcut over riding the full U-shaped arc on Line 1.
///
/// Network:
///   Line 1 (arc):      Circle(A) → Rectangle(B) → Triangle(C) → Diamond(D) → Star(E)
///   Line 2 (shortcut): Rectangle(B) → Star(E)
///
/// Key distances (Chebyshev tile-steps):
///   A(2,7)→B(2,2): 5   B(2,2)→C(7,2): 5   C(7,2)→D(12,2): 5   D(12,2)→E(12,7): 5
///   B(2,2)→E(12,7): 10  (diagonal shortcut on Line 2)
///
/// Transfer scenario for a passenger at A wanting Star(E):
///   • Boards Line 1 at A — B is the next stop and is on the global optimum (A→B→E, 15 steps).
///   • At B the transfer-drop fires: continuing to C costs 5+10=15 steps, but the
///     global optimum from B is only 10 (direct to E on Line 2).  15 ≠ 10 → drop.
///   • Passenger waits at B, boards Line 2, and is delivered directly to E.
///   Total: 15 steps via transfer  vs  20 steps via full arc.
///
/// Setup (one action per tick after all stations have spawned):
///   Phase 0  CreateLine (resource)  A → B          (Line 1, first segment)
///   Phase 1  Extend Line 1          B → C
///   Phase 2  Extend Line 1          C → D
///   Phase 3  Extend Line 1          D → E
///   Phase 4  CreateLine (resource)  B → E          (Line 2, the shortcut)
///   Phase 5  AddVehicleToLine       Train 1 → Line 1 at A
///   Phase 6  AddVehicleToLine       Train 2 → Line 2 at B
/// </summary>
internal class ShortcutTransferRunner : IMetroManiaRunner
{
    private Guid? _circleId;
    private Guid? _rectangleId;
    private Guid? _triangleId;
    private Guid? _diamondId;
    private Guid? _starId;

    private Guid? _line1Id;

    private int _setupPhase;

    public PlayerAction OnHourTicked(GameSnapshot snapshot)
    {
        if (_circleId is null || _rectangleId is null || _triangleId is null ||
            _diamondId is null || _starId is null)
            return new NoAction();

        var availableLines  = snapshot.Resources.Where(r => r.Type == ResourceType.Line  && !r.InUse).ToList();
        var availableTrains = snapshot.Resources.Where(r => r.Type == ResourceType.Train && !r.InUse).ToList();

        switch (_setupPhase)
        {
            case 0:
            {
                // Create Line 1: A(Circle) → B(Rectangle)
                if (availableLines.Count < 1) return new NoAction();
                var lineResourceId = availableLines[0].Id;
                _setupPhase++;
                return new CreateLine(lineResourceId, _circleId.Value, _rectangleId.Value);
            }

            case 1:
            {
                // Extend Line 1: B(Rectangle) → C(Triangle)
                // Look up Line 1 by the Circle station (unique to Line 1)
                _line1Id ??= snapshot.Lines.FirstOrDefault(l => l.StationIds.Contains(_circleId!.Value))?.LineId;
                if (_line1Id is null) return new NoAction();
                _setupPhase++;
                return new CreateLine(_line1Id.Value, _rectangleId.Value, _triangleId.Value);
            }

            case 2:
            {
                // Extend Line 1: C(Triangle) → D(Diamond)
                if (_line1Id is null) return new NoAction();
                _setupPhase++;
                return new CreateLine(_line1Id.Value, _triangleId.Value, _diamondId.Value);
            }

            case 3:
            {
                // Extend Line 1: D(Diamond) → E(Star)
                if (_line1Id is null) return new NoAction();
                _setupPhase++;
                return new CreateLine(_line1Id.Value, _diamondId.Value, _starId.Value);
            }

            case 4:
            {
                // Create Line 2 (shortcut): B(Rectangle) → E(Star)
                if (availableLines.Count < 1) return new NoAction();
                _setupPhase++;
                return new CreateLine(availableLines[0].Id, _rectangleId.Value, _starId.Value);
            }

            case 5:
            {
                // Deploy Train 1 on Line 1, starting at A(Circle)
                if (availableTrains.Count < 1 || _line1Id is null) return new NoAction();
                _setupPhase++;
                return new AddVehicleToLine(availableTrains[0].Id, _line1Id.Value, _circleId.Value);
            }

            case 6:
            {
                // Deploy Train 2 on Line 2 (shortcut), starting at B(Rectangle)
                if (availableTrains.Count < 1) return new NoAction();
                var line2 = FindLine2(snapshot);
                if (line2 is null) return new NoAction();
                _setupPhase++;
                return new AddVehicleToLine(availableTrains[0].Id, line2.LineId, _rectangleId.Value);
            }

            default:
                return new NoAction();
        }
    }

    // Line 2 = the shortcut that has Rectangle and Star but NOT Circle
    private Line? FindLine2(GameSnapshot snapshot) =>
        snapshot.Lines.FirstOrDefault(l =>
            l.StationIds.Contains(_rectangleId!.Value) &&
            l.StationIds.Contains(_starId!.Value) &&
            !l.StationIds.Contains(_circleId!.Value));

    public void OnStationSpawned(GameSnapshot snapshot, Guid stationId, Location location, StationType stationType)
    {
        switch (stationType)
        {
            case StationType.Circle:    _circleId    = stationId; break;
            case StationType.Rectangle: _rectangleId = stationId; break;
            case StationType.Triangle:  _triangleId  = stationId; break;
            case StationType.Diamond:   _diamondId   = stationId; break;
            case StationType.Star:      _starId      = stationId; break;
        }
    }

    public void OnDayStart(GameSnapshot snapshot) { }

    public void OnWeeklyGiftReceived(GameSnapshot snapshot, ResourceType gift) { }

    public void OnPassengerSpawned(GameSnapshot snapshot, Guid stationId, Guid passengerId) { }

    public void OnStationCrowded(GameSnapshot snapshot, Guid stationId, int numberOfPassengersWaiting) { }

    public void OnGameOver(GameSnapshot snapshot, Guid stationId) { }

    public void OnInvalidPlayerAction(GameSnapshot snapshot, int code, string description) =>
        Console.WriteLine($"[Level5] Invalid action (code {code}): {description}");

    public void OnVehicleRemoved(GameSnapshot snapshot, Guid vehicleId) { }
}
