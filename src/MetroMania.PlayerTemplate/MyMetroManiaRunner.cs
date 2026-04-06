using MetroMania.Domain.Enums;
using MetroMania.Engine.Contracts;
using MetroMania.Engine.Model;

namespace MetroMania.PlayerTemplate;

public class MyMetroManiaRunner : IMetroManiaRunner
{
    public PlayerAction OnHourTicked(GameSnapshot snapshot) => PlayerAction.None;
    public void OnDayStart(GameSnapshot snapshot) { }
    public void OnWeeklyGiftReceived(GameSnapshot snapshot, ResourceType gift) { }
    public void OnStationSpawned(GameSnapshot snapshot, Guid stationId, Location location, StationType stationType) { }
    public void OnPassengerSpawned(GameSnapshot snapshot, Guid stationId, Guid passengerId) { }
    public void OnStationCrowded(GameSnapshot snapshot, Guid stationId, int numberOfPassengersWaiting) { }
    public void OnGameOver(GameSnapshot snapshot, Guid stationId) { }
    public void OnInvalidPlayerAction(GameSnapshot snapshot, int code, string description) { }
    public void OnVehicleRemoved(GameSnapshot snapshot, Guid vehicleId) { }
    public void OnLineRemoved(GameSnapshot snapshot, Guid lineId) { }
}