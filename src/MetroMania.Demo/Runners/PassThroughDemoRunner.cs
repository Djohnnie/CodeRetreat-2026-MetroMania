using MetroMania.Domain.Enums;
using MetroMania.Engine.Contracts;
using MetroMania.Engine.Model;

namespace MetroMania.Demo.Runners;

/// <summary>
/// Runner for Level 8 — builds a network where Line 1 passes straight through
/// Triangle(B) without connecting to it, demonstrating the split-line rendering.
///
/// Network:
///   Line 1: Circle(A, 2,5) → Rectangle(C, 14,5)     (horizontal, crosses B's tile)
///   Line 2: Triangle(B, 8,5) → Star(D, 8,2)          (vertical, serves B and D)
///
/// Setup (one action per tick after all stations have spawned):
///   Phase 0  CreateLine (resource)   A → C     (Line 1 — passes through B's tile)
///   Phase 1  CreateLine (resource)   B → D     (Line 2 — serves the skipped station)
///   Phase 2  AddVehicleToLine        Train 1 → Line 1 at A
///   Phase 3  AddVehicleToLine        Train 2 → Line 2 at B
/// </summary>
internal class PassThroughDemoRunner : IMetroManiaRunner
{
    private Guid? _circleId;     // A
    private Guid? _triangleId;   // B (pass-through)
    private Guid? _rectangleId;  // C
    private Guid? _starId;       // D

    private Guid? _line1Id;
    private Guid? _line2Id;

    private int _setupPhase;

    public PlayerAction OnHourTicked(GameSnapshot snapshot)
    {
        if (_circleId is null || _triangleId is null || _rectangleId is null || _starId is null)
            return new NoAction();

        var availableLines  = snapshot.Resources.Where(r => r.Type == ResourceType.Line  && !r.InUse).ToList();
        var availableTrains = snapshot.Resources.Where(r => r.Type == ResourceType.Train && !r.InUse).ToList();

        switch (_setupPhase)
        {
            case 0:
            {
                // Line 1: A(Circle) → C(Rectangle) — passes through B's tile at (8,5)
                if (availableLines.Count < 1) return new NoAction();
                _setupPhase++;
                return new CreateLine(availableLines[0].Id, _circleId.Value, _rectangleId.Value);
            }

            case 1:
            {
                // Line 2: B(Triangle) → D(Star) — serves the station that Line 1 skips
                _line1Id ??= snapshot.Lines.FirstOrDefault(l => l.StationIds.Contains(_circleId!.Value))?.LineId;
                if (availableLines.Count < 1) return new NoAction();
                _setupPhase++;
                return new CreateLine(availableLines[0].Id, _triangleId.Value, _starId.Value);
            }

            case 2:
            {
                // Deploy Train 1 on Line 1 at A(Circle)
                _line1Id ??= snapshot.Lines.FirstOrDefault(l => l.StationIds.Contains(_circleId!.Value))?.LineId;
                if (availableTrains.Count < 1 || _line1Id is null) return new NoAction();
                _setupPhase++;
                return new AddVehicleToLine(availableTrains[0].Id, _line1Id.Value, _circleId.Value);
            }

            case 3:
            {
                // Deploy Train 2 on Line 2 at B(Triangle)
                _line2Id ??= snapshot.Lines.FirstOrDefault(l => l.StationIds.Contains(_triangleId!.Value))?.LineId;
                if (availableTrains.Count < 1 || _line2Id is null) return new NoAction();
                _setupPhase++;
                return new AddVehicleToLine(availableTrains[0].Id, _line2Id.Value, _triangleId.Value);
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
        Console.WriteLine($"[Level8] Invalid action (code {code}): {description}");

    public void OnVehicleRemoved(GameSnapshot snapshot, Guid vehicleId) { }
    public void OnLineRemoved(GameSnapshot snapshot, Guid lineId) { }
}
