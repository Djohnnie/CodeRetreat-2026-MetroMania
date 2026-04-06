using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;
using MetroMania.Engine.Contracts;
using MetroMania.Engine.Model;
using Moq;

namespace MetroMania.Engine.Tests.Support;

public record StationSpawnedEvent(GameTime Time, Guid StationId, Location Location, StationType StationType);
public record PassengerSpawnedEvent(GameTime Time, Guid StationId, Guid PassengerId, StationType OriginStationType, StationType DestinationType);
public record WeeklyGiftEvent(GameTime Time, ResourceType Gift);
public record OverrunEvent(GameTime Time, Guid StationId, int PassengerCount);
public record GameOverEvent(GameTime Time, Guid StationId);
public record InvalidActionEvent(GameTime Time, int Code, string Description);

/// <summary>
/// Shared scenario context holding the engine, mock runner, level configuration,
/// simulation results, and a full event log captured via Moq callbacks.
/// Reqnroll injects one instance per scenario and shares it across all step definition classes.
/// </summary>
public class EngineTestContext
{
    public MetroManiaEngine Engine { get; } = new();
    public Mock<IMetroManiaRunner> Runner { get; }

    // Level configuration
    public List<MetroStation> Stations { get; } = [];
    public List<WeeklyGiftOverride> WeeklyGiftOverrides { get; } = [];
    public List<ResourceType> InitialResources { get; } = [];
    public int Seed { get; set; } = 42;
    public int VehicleCapacity { get; set; } = 6;

    // Pending player actions dequeued one per tick from OnHourTicked
    public Queue<Func<GameSnapshot, PlayerAction>> PendingActions { get; } = new();

    // Simulation results
    public SimulationResult? SimResult { get; set; }
    public GameSnapshot? LastSnapshot => SimResult?.GameSnapshots.LastOrDefault();

    // Ordered log of all event names fired across all ticks
    public List<string> EventLog { get; } = [];

    // Typed event tracking
    public List<GameTime> DayStartCalls { get; } = [];
    public List<GameTime> HourTickCalls { get; } = [];
    public List<StationSpawnedEvent> StationSpawnedCalls { get; } = [];
    public List<PassengerSpawnedEvent> PassengerSpawnedCalls { get; } = [];
    public List<WeeklyGiftEvent> WeeklyGiftCalls { get; } = [];
    public List<OverrunEvent> OverrunCalls { get; } = [];
    public List<GameOverEvent> GameOverCalls { get; } = [];
    public List<InvalidActionEvent> InvalidActionCalls { get; } = [];

    // Station ID lookup: Location → Guid (populated by OnStationSpawned callback)
    public Dictionary<Location, Guid> StationIdsByLocation { get; } = [];

    // Determinism support: sequences saved from a prior run for comparison
    public List<ResourceType> PreviousWeeklyGifts { get; } = [];
    public int PreviousPassengerSpawnCount { get; set; }

    public EngineTestContext()
    {
        Runner = new Mock<IMetroManiaRunner>();

        Runner.Setup(r => r.OnDayStart(It.IsAny<GameSnapshot>()))
            .Callback<GameSnapshot>(snapshot =>
            {
                EventLog.Add("OnDayStart");
                DayStartCalls.Add(snapshot.Time);
            });

        Runner.Setup(r => r.OnStationSpawned(It.IsAny<GameSnapshot>(), It.IsAny<Guid>(), It.IsAny<Location>(), It.IsAny<StationType>()))
            .Callback<GameSnapshot, Guid, Location, StationType>((snapshot, id, loc, type) =>
            {
                EventLog.Add("OnStationSpawned");
                StationIdsByLocation[loc] = id;
                StationSpawnedCalls.Add(new StationSpawnedEvent(snapshot.Time, id, loc, type));
            });

        Runner.Setup(r => r.OnPassengerSpawned(It.IsAny<GameSnapshot>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .Callback<GameSnapshot, Guid, Guid>((snapshot, stationId, passengerId) =>
            {
                EventLog.Add("OnPassengerSpawned");
                var station = snapshot.Stations.Values.FirstOrDefault(s => s.Id == stationId);
                var passenger = snapshot.Passengers.FirstOrDefault(p => p.Id == passengerId);
                PassengerSpawnedCalls.Add(new PassengerSpawnedEvent(
                    snapshot.Time,
                    stationId,
                    passengerId,
                    station?.StationType ?? default,
                    passenger?.DestinationType ?? default));
            });

        Runner.Setup(r => r.OnWeeklyGiftReceived(It.IsAny<GameSnapshot>(), It.IsAny<ResourceType>()))
            .Callback<GameSnapshot, ResourceType>((snapshot, gift) =>
            {
                EventLog.Add("OnWeeklyGiftReceived");
                WeeklyGiftCalls.Add(new WeeklyGiftEvent(snapshot.Time, gift));
            });

        Runner.Setup(r => r.OnStationCrowded(It.IsAny<GameSnapshot>(), It.IsAny<Guid>(), It.IsAny<int>()))
            .Callback<GameSnapshot, Guid, int>((snapshot, stationId, count) =>
            {
                EventLog.Add("OnStationCrowded");
                OverrunCalls.Add(new OverrunEvent(snapshot.Time, stationId, count));
            });

        Runner.Setup(r => r.OnGameOver(It.IsAny<GameSnapshot>(), It.IsAny<Guid>()))
            .Callback<GameSnapshot, Guid>((snapshot, stationId) =>
            {
                EventLog.Add("OnGameOver");
                GameOverCalls.Add(new GameOverEvent(snapshot.Time, stationId));
            });

        Runner.Setup(r => r.OnInvalidPlayerAction(It.IsAny<GameSnapshot>(), It.IsAny<int>(), It.IsAny<string>()))
            .Callback<GameSnapshot, int, string>((snapshot, code, description) =>
            {
                EventLog.Add("OnInvalidPlayerAction");
                InvalidActionCalls.Add(new InvalidActionEvent(snapshot.Time, code, description));
            });

        Runner.Setup(r => r.OnHourTicked(It.IsAny<GameSnapshot>()))
            .Returns<GameSnapshot>(snapshot =>
            {
                EventLog.Add("OnHourTicked");
                HourTickCalls.Add(snapshot.Time);
                if (PendingActions.Count > 0)
                    return PendingActions.Dequeue()(snapshot);
                return PlayerAction.None;
            });
    }

    public Level BuildLevel() => new()
    {
        Title = "Test",
        GridWidth = 10,
        GridHeight = 10,
        LevelData = new LevelData
        {
            Seed = Seed,
            VehicleCapacity = VehicleCapacity,
            Stations = [.. Stations],
            WeeklyGiftOverrides = [.. WeeklyGiftOverrides],
            InitialResources = [.. InitialResources]
        }
    };

    /// <summary>
    /// Saves the current weekly gift sequence and resets all event tracking so the
    /// simulation can be run again for comparison (determinism tests).
    /// </summary>
    public void PrepareForRerun()
    {
        PreviousWeeklyGifts.AddRange(WeeklyGiftCalls.Select(e => e.Gift));
        PreviousPassengerSpawnCount = PassengerSpawnedCalls.Count;
        PendingActions.Clear();
        EventLog.Clear();
        DayStartCalls.Clear();
        HourTickCalls.Clear();
        StationSpawnedCalls.Clear();
        PassengerSpawnedCalls.Clear();
        WeeklyGiftCalls.Clear();
        OverrunCalls.Clear();
        GameOverCalls.Clear();
        InvalidActionCalls.Clear();
        StationIdsByLocation.Clear();
        SimResult = null;
    }
}
