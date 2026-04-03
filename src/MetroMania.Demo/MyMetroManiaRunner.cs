using MetroMania.Domain.Enums;
using MetroMania.Engine.Contracts;
using MetroMania.Engine.Model;

namespace MetroMania.Demo;

public class MyMetroManiaRunner : IMetroManiaRunner
{
    public PlayerAction OnHourTicked(GameSnapshot snapshot) => PlayerAction.None;
    public void OnDayStart(GameSnapshot snapshot) { }
    public void OnWeeklyGiftReceived(GameSnapshot snapshot, ResourceType gift) { }
    public void OnStationSpawned(GameSnapshot snapshot, Guid stationId, Location location, StationType stationType) { }
    public void OnPassengerSpawned(GameSnapshot snapshot, Guid stationId, Guid passengerId) { }
    public void OnStationOverrun(GameSnapshot snapshot, Guid stationId, int numberOfPassengersWaiting) { }
    public void OnGameOver(GameSnapshot snapshot, Guid stationId) { }
}