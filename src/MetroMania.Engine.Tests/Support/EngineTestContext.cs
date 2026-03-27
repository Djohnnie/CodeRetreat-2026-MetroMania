using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;
using MetroMania.Engine.Contracts;
using MetroMania.Engine.Model;
using Moq;

namespace MetroMania.Engine.Tests.Support;

public record PassengerWaitingEvent(GameTime Time, Location Location, IReadOnlyList<Passenger> Passengers);

public record WeeklyGiftEvent(GameSnapshot Snapshot, ResourceType Gift);

/// <summary>
/// Shared scenario context holding the engine, mock runner, level configuration,
/// simulation results, and a full event log captured via Moq callbacks.
/// Reqnroll injects one instance per scenario and shares it across all step definition classes.
/// </summary>
public class EngineTestContext
{
    public MetroManiaEngine Engine { get; } = new();
    public List<MetroStation> Stations { get; } = [];
    public List<WeeklyGiftOverride> WeeklyGiftOverrides { get; } = [];
    public Mock<IMetroManiaRunner> Runner { get; }

    // Simulation results
    public GameSnapshot? Snapshot { get; set; }
    public GameResult? Result { get; set; }

    // Event tracking — always active
    public List<string> EventLog { get; } = [];
    public List<GameTime> DayStartCalls { get; } = [];
    public List<GameTime> HourTickCalls { get; } = [];

    // Passenger spawn tracking
    public List<PassengerWaitingEvent> PassengerWaitingCalls { get; } = [];

    // Weekly gift tracking for determinism tests
    public int Seed { get; set; } = 42;
    public List<ResourceType> WeeklyGiftTypes { get; } = [];
    public List<ResourceType> PreviousWeeklyGiftTypes { get; } = [];
    public List<WeeklyGiftEvent> WeeklyGiftEvents { get; } = [];

    // Cancellation support
    public CancellationTokenSource Cts { get; } = new();
    public int? CancelAfterHours { get; set; }
    public bool WasCancelled { get; set; }

    public EngineTestContext()
    {
        Runner = new Mock<IMetroManiaRunner>();

        Runner.Setup(r => r.OnStationSpawned(It.IsAny<GameSnapshot>(), It.IsAny<Guid>(), It.IsAny<Location>(), It.IsAny<StationType>()))
            .Callback(() => EventLog.Add("OnStationSpawned"));
        Runner.Setup(r => r.OnWeeklyGift(It.IsAny<GameSnapshot>(), It.IsAny<ResourceType>()))
            .Callback<GameSnapshot, ResourceType>((snapshot, gift) =>
            {
                EventLog.Add("OnWeeklyGift");
                WeeklyGiftTypes.Add(gift);
                WeeklyGiftEvents.Add(new WeeklyGiftEvent(snapshot, gift));
            });
        Runner.Setup(r => r.OnPassengerWaiting(It.IsAny<GameSnapshot>(), It.IsAny<Location>(), It.IsAny<IReadOnlyList<Passenger>>()))
            .Callback<GameSnapshot, Location, IReadOnlyList<Passenger>>((snapshot, loc, passengers) =>
            {
                EventLog.Add("OnPassengerWaiting");
                PassengerWaitingCalls.Add(new PassengerWaitingEvent(snapshot.Time, loc, passengers.ToList().AsReadOnly()));
            });
        Runner.Setup(r => r.OnStationOverrun(It.IsAny<GameSnapshot>(), It.IsAny<Location>(), It.IsAny<IReadOnlyList<Passenger>>()))
            .Callback(() => EventLog.Add("OnStationOverrun"));
        Runner.Setup(r => r.OnGameOver(It.IsAny<GameSnapshot>(), It.IsAny<Location>(), It.IsAny<IReadOnlyList<Passenger>>()))
            .Callback(() => EventLog.Add("OnGameOver"));
        Runner.Setup(r => r.OnDayStart(It.IsAny<GameSnapshot>()))
            .Callback<GameSnapshot>(snapshot =>
            {
                DayStartCalls.Add(snapshot.Time);
                EventLog.Add("OnDayStart");
            });
        Runner.Setup(r => r.OnHourTick(It.IsAny<GameSnapshot>()))
            .Callback<GameSnapshot>(snapshot =>
            {
                HourTickCalls.Add(snapshot.Time);
                EventLog.Add("OnHourTick");
                if (CancelAfterHours.HasValue && HourTickCalls.Count >= CancelAfterHours.Value)
                    Cts.Cancel();
            })
            .Returns(PlayerAction.None);
    }

    public Level BuildLevel() => new()
    {
        Title = "Test",
        GridWidth = 10,
        GridHeight = 10,
        LevelData = new LevelData
        {
            Seed = Seed,
            Stations = [.. Stations],
            WeeklyGiftOverrides = [.. WeeklyGiftOverrides]
        }
    };

    /// <summary>
    /// Saves the current weekly gift sequence and resets tracking state
    /// so the simulation can run again for comparison.
    /// </summary>
    public void PrepareForRerun()
    {
        PreviousWeeklyGiftTypes.AddRange(WeeklyGiftTypes);
        WeeklyGiftTypes.Clear();
        WeeklyGiftEvents.Clear();
        EventLog.Clear();
        DayStartCalls.Clear();
        HourTickCalls.Clear();
        PassengerWaitingCalls.Clear();
        Snapshot = null;
        Result = null;
        WasCancelled = false;
    }
}
