using MetroMania.Domain.Enums;
using MetroMania.Engine.Contracts;
using MetroMania.Engine.Model;

namespace MetroMania.Demo;

public class MyMetroManiaRunner : IMetroManiaRunner
{
    public PlayerAction OnHourTick(int day, int hour) => PlayerAction.None;

    public void OnDayStart(int day, DayOfWeek dayOfWeek) { }

    public void OnWeeklyGift(ResourceType gift) { }

    public void OnStationSpawned(Location location, StationType stationType) { }

    public void OnPassengerWaiting(Location location, IReadOnlyList<Passenger> passengers) { }

    public void OnStationOverrun(Location location, IReadOnlyList<Passenger> passengers) { }

    public void OnGameOver(Location location, IReadOnlyList<Passenger> passengers) { }
}