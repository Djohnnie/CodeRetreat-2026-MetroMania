using MetroMania.Domain.Enums;
using MetroMania.Engine.Contracts;
using MetroMania.Engine.Model;

namespace MetroMania.Demo;

public class MyMetroManiaRunner : IMetroManiaRunner
{
    public PlayerAction OnHourTick(GameSnapshot snapshot) => PlayerAction.None;

    public void OnDayStart(GameSnapshot snapshot) { }

    public void OnWeeklyGift(GameSnapshot snapshot, ResourceType gift) { }

    public void OnStationSpawned(GameSnapshot snapshot, Guid stationId, Location location, StationType stationType) { }

    public void OnPassengerWaiting(GameSnapshot snapshot, Location location, IReadOnlyList<Passenger> passengers) { }

    public void OnStationOverrun(GameSnapshot snapshot, Location location, IReadOnlyList<Passenger> passengers) { }

    public void OnGameOver(GameSnapshot snapshot, Location location, IReadOnlyList<Passenger> passengers) { }
}