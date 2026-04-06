using MetroMania.Domain.Enums;
using MetroMania.Engine.Contracts;
using MetroMania.Engine.Model;

namespace MetroMania.Demo.Runners;

/// <summary>
/// A runner designed specifically for Level 4 to demonstrate the transfer-routing feature.
///
/// Network layout:
///   Line 1 (horizontal): Circle ——— Rectangle ——— Star
///   Line 2 (vertical):              Rectangle
///                                       |
///                                   Triangle
///
/// Setup sequence (one action per tick):
///   1. CreateLine (resource)           Circle → Rectangle          (Line 1 starts)
///   2. ExtendLineFromTerminal          Rectangle → Star            (Line 1 completes)
///   3. CreateLine (resource)           Rectangle → Triangle        (Line 2)
///   4. AddVehicleToLine       Train 1 → Line 1 at Circle
///   5. AddVehicleToLine       Train 2 → Line 2 at Triangle
///
/// With this network, a passenger at Circle wanting Triangle will:
///   - Board the Line 1 train (Rectangle is on the globally optimal path)
///   - Be dropped at Rectangle by the engine's transfer-drop logic
///     (Triangle is not on Line 1, so MinStepsViaLine = ∞ > ShortestSteps)
///   - Be collected at Rectangle by the Line 2 train and delivered to Triangle
/// </summary>
internal class TransferDemoRunner : IMetroManiaRunner
{
    // Station IDs discovered via OnStationSpawned
    private Guid? _circleId;
    private Guid? _rectangleId;
    private Guid? _starId;
    private Guid? _triangleId;

    /// <summary>
    /// Setup phase counter — advances by one each time an action is successfully
    /// issued so that exactly one setup action is emitted per tick.
    /// 0 → create Line 1 (Circle→Rectangle)
    /// 1 → extend Line 1 (Rectangle→Star)
    /// 2 → create Line 2 (Rectangle→Triangle)
    /// 3 → deploy Train 1 on Line 1 at Circle
    /// 4 → deploy Train 2 on Line 2 at Triangle
    /// 5 → done, emit NoAction every tick
    /// </summary>
    private int _setupPhase;

    public PlayerAction OnHourTicked(GameSnapshot snapshot)
    {
        // Wait until all four stations have spawned
        if (_circleId is null || _rectangleId is null || _starId is null || _triangleId is null)
            return new NoAction();

        var availableLines  = snapshot.Resources.Where(r => r.Type == ResourceType.Line  && !r.InUse).ToList();
        var availableTrains = snapshot.Resources.Where(r => r.Type == ResourceType.Train && !r.InUse).ToList();

        switch (_setupPhase)
        {
            case 0:
            {
                // Create Line 1: Circle → Rectangle  (consumes one Line resource)
                if (availableLines.Count < 1) return new NoAction();
                _setupPhase++;
                return new CreateLine(availableLines[0].Id, _circleId.Value, _rectangleId.Value);
            }

            case 1:
            {
                // Extend Line 1: Rectangle → Star  (no extra resource consumed)
                var line1 = snapshot.Lines.FirstOrDefault(l =>
                    l.StationIds.Contains(_circleId.Value) && l.StationIds.Contains(_rectangleId.Value));
                if (line1 is null) return new NoAction();
                _setupPhase++;
                return new ExtendLineFromTerminal(line1.LineId, _rectangleId.Value, _starId.Value);
            }

            case 2:
            {
                // Create Line 2: Rectangle → Triangle  (consumes one Line resource)
                if (availableLines.Count < 1) return new NoAction();
                _setupPhase++;
                return new CreateLine(availableLines[0].Id, _rectangleId.Value, _triangleId.Value);
            }

            case 3:
            {
                // Deploy Train 1 on Line 1 starting at Circle
                if (availableTrains.Count < 1) return new NoAction();
                var line1 = snapshot.Lines.FirstOrDefault(l =>
                    l.StationIds.Contains(_circleId.Value) && l.StationIds.Contains(_starId.Value));
                if (line1 is null) return new NoAction();
                _setupPhase++;
                return new AddVehicleToLine(availableTrains[0].Id, line1.LineId, _circleId.Value);
            }

            case 4:
            {
                // Deploy Train 2 on Line 2 starting at Triangle
                if (availableTrains.Count < 1) return new NoAction();
                var line2 = snapshot.Lines.FirstOrDefault(l =>
                    l.StationIds.Contains(_triangleId.Value) &&
                    l.StationIds.Contains(_rectangleId.Value) &&
                    !l.StationIds.Contains(_circleId.Value));
                if (line2 is null) return new NoAction();
                _setupPhase++;
                return new AddVehicleToLine(availableTrains[0].Id, line2.LineId, _triangleId.Value);
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
            case StationType.Rectangle: _rectangleId = stationId; break;
            case StationType.Star:      _starId      = stationId; break;
            case StationType.Triangle:  _triangleId  = stationId; break;
        }
    }

    public void OnDayStart(GameSnapshot snapshot) { }

    public void OnWeeklyGiftReceived(GameSnapshot snapshot, ResourceType gift) { }

    public void OnPassengerSpawned(GameSnapshot snapshot, Guid stationId, Guid passengerId) { }

    public void OnStationCrowded(GameSnapshot snapshot, Guid stationId, int numberOfPassengersWaiting) { }

    public void OnGameOver(GameSnapshot snapshot, Guid stationId) { }

    public void OnInvalidPlayerAction(GameSnapshot snapshot, int code, string description) =>
        Console.WriteLine($"[Level4] Invalid action (code {code}): {description}");

    public void OnVehicleRemoved(GameSnapshot snapshot, Guid vehicleId) { }
    public void OnLineRemoved(GameSnapshot snapshot, Guid lineId) { }
}