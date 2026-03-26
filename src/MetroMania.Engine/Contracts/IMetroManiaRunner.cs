using MetroMania.Domain.Enums;
using MetroMania.Engine.Model;

namespace MetroMania.Engine.Contracts;

public interface IMetroManiaRunner
{
    /// <summary>
    /// Called every hour of the game. The player must return an action to perform.
    /// </summary>
    PlayerAction OnHourTick(GameTime time);

    /// <summary>
    /// Called at the start of each new day with the current game time.
    /// </summary>
    void OnDayStart(GameTime time);

    /// <summary>
    /// Called every Monday at 0h when the player receives a new resource.
    /// </summary>
    void OnWeeklyGift(GameTime time, ResourceType gift);

    /// <summary>
    /// Called when a new station appears on the map.
    /// </summary>
    void OnStationSpawned(GameTime time, Location location, StationType stationType);

    /// <summary>
    /// Called when a station has a new passenger waiting to be picked up.
    /// </summary>
    void OnPassengerWaiting(GameTime time, Location location, IReadOnlyList<Passenger> passengers);

    /// <summary>
    /// Called when a station is getting overrun by passengers (10+).
    /// </summary>
    void OnStationOverrun(GameTime time, Location location, IReadOnlyList<Passenger> passengers);

    /// <summary>
    /// Called when the game is over because a station has too many passengers not picked up (20+).
    /// </summary>
    void OnGameOver(GameTime time, Location location, IReadOnlyList<Passenger> passengers);
}