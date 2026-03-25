using MetroMania.Domain.Enums;
using MetroMania.Engine.Model;

namespace MetroMania.Engine.Contracts;

public interface IMetroManiaRunner
{
    /// <summary>
    /// Called every hour of the game. The player must return an action to perform.
    /// </summary>
    PlayerAction OnHourTick(int day, int hour);

    /// <summary>
    /// Called at the start of each new day with the current day of the week.
    /// </summary>
    void OnDayStart(int day, DayOfWeek dayOfWeek);

    /// <summary>
    /// Called every Monday at 0h when the player receives a new resource.
    /// </summary>
    void OnWeeklyGift(ResourceType gift);

    /// <summary>
    /// Called when a new station appears on the map.
    /// </summary>
    void OnStationSpawned(Location location, StationType stationType);

    /// <summary>
    /// Called when a station has a new passenger waiting to be picked up.
    /// </summary>
    void OnPassengerWaiting(Location location, IReadOnlyList<Passenger> passengers);

    /// <summary>
    /// Called when a station is getting overrun by passengers (10+).
    /// </summary>
    void OnStationOverrun(Location location, IReadOnlyList<Passenger> passengers);

    /// <summary>
    /// Called when the game is over because a station has too many passengers not picked up (20+).
    /// </summary>
    void OnGameOver(Location location, IReadOnlyList<Passenger> passengers);
}