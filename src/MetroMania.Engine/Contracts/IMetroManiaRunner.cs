using MetroMania.Domain.Enums;
using MetroMania.Engine.Model;

namespace MetroMania.Engine.Contracts;

/// <summary>
/// Callback interface that player bots must implement.
/// The engine invokes these methods during the simulation to notify the bot of events and request actions.
/// </summary>
public interface IMetroManiaRunner
{
    /// <summary>
    /// Called at the end of every hour in the game engine.
    /// The player must return an action to perform.
    /// </summary>
    PlayerAction OnHourTicked(GameSnapshot snapshot);

    /// <summary>
    /// Called at the start of each new day with the current game snapshot.
    /// </summary>
    void OnDayStart(GameSnapshot snapshot);

    /// <summary>
    /// Called every Monday at 0h when the player receives a new resource.
    /// </summary>
    void OnWeeklyGiftReceived(GameSnapshot snapshot, ResourceType gift);

    /// <summary>
    /// Called when a new station appears on the map.
    /// </summary>
    void OnStationSpawned(GameSnapshot snapshot, Guid stationId, Location location, StationType stationType);

    /// <summary>
    /// Called when a station has a new passenger waiting to be picked up.
    /// </summary>
    void OnPassengerSpawned(GameSnapshot snapshot, Guid stationId, Guid passengerId);

    /// <summary>
    /// Called when a station is getting crowded (10+ passengers waiting).
    /// </summary>
    void OnStationCrowded(GameSnapshot snapshot, Guid stationId, int numberOfPassengersWaiting);

    /// <summary>
    /// Called when the game is over because a station has too many passengers not picked up (20+).
    /// </summary>
    void OnGameOver(GameSnapshot snapshot, Guid stationId);

    /// <summary>
    /// Called when a player action returned from <see cref="OnHourTicked"/> was invalid
    /// and had no effect on the game state.
    /// <para>
    /// Use <see cref="PlayerActionError"/> for the well-known <paramref name="code"/> values.
    /// </para>
    /// </summary>
    /// <param name="snapshot">The snapshot at the time the action was attempted.</param>
    /// <param name="code">A numeric code identifying the violation (see <see cref="PlayerActionError"/>).</param>
    /// <param name="description">A human-readable explanation of why the action was rejected.</param>
    void OnInvalidPlayerAction(GameSnapshot snapshot, int code, string description);

    /// <summary>
    /// Called after a train with a pending removal has dropped off all its passengers
    /// and has been physically removed from the map. The train resource is now available
    /// for redeployment.
    /// </summary>
    /// <param name="snapshot">The snapshot after the train was removed.</param>
    /// <param name="vehicleId">The Id of the removed train (matches the resource Id).</param>
    void OnVehicleRemoved(GameSnapshot snapshot, Guid vehicleId);

    /// <summary>
    /// Called after a line with a pending removal has had all its trains removed
    /// and has been physically removed from the map. The line resource is now available
    /// for redeployment.
    /// </summary>
    /// <param name="snapshot">The snapshot after the line was removed.</param>
    /// <param name="lineId">The Id of the removed line (matches the resource Id).</param>
    void OnLineRemoved(GameSnapshot snapshot, Guid lineId);
}