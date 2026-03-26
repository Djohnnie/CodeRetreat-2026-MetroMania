using MetroMania.Domain.Enums;
using MetroMania.Engine.Contracts;
using MetroMania.Engine.Model;

namespace MetroMania.Demo;

public class MyMetroManiaRunner : IMetroManiaRunner
{
    public PlayerAction OnHourTick(GameTime time) => PlayerAction.None;

    public void OnDayStart(GameTime time) { }

    public void OnWeeklyGift(GameTime time, ResourceType gift) { }

    public void OnStationSpawned(GameTime time, Location location, StationType stationType) { }

    public void OnPassengerWaiting(GameTime time, Location location, IReadOnlyList<Passenger> passengers) { }

    public void OnStationOverrun(GameTime time, Location location, IReadOnlyList<Passenger> passengers) { }

    public void OnGameOver(GameTime time, Location location, IReadOnlyList<Passenger> passengers) { }
}