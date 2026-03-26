using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;
using MetroMania.Engine.Contracts;
using MetroMania.Engine.Model;
using Moq;

namespace MetroMania.Engine.Tests.Support;

/// <summary>
/// Shared scenario context holding the engine, mock runner, level configuration,
/// simulation results, and a full event log captured via Moq callbacks.
/// Reqnroll injects one instance per scenario and shares it across all step definition classes.
/// </summary>
public class EngineTestContext
{
    public MetroManiaEngine Engine { get; } = new();
    public List<MetroStation> Stations { get; } = [];
    public Mock<IMetroManiaRunner> Runner { get; }

    // Simulation results
    public GameSnapshot? Snapshot { get; set; }
    public GameResult? Result { get; set; }

    // Event tracking — always active
    public List<string> EventLog { get; } = [];
    public List<GameTime> DayStartCalls { get; } = [];
    public List<GameTime> HourTickCalls { get; } = [];

    // Weekly gift tracking for determinism tests
    public int Seed { get; set; } = 42;
    public List<ResourceType> WeeklyGiftTypes { get; } = [];
    public List<ResourceType> PreviousWeeklyGiftTypes { get; } = [];

    public EngineTestContext()
    {
        Runner = new Mock<IMetroManiaRunner>();

        Runner.Setup(r => r.OnStationSpawned(It.IsAny<GameTime>(), It.IsAny<Location>(), It.IsAny<StationType>()))
            .Callback(() => EventLog.Add("OnStationSpawned"));
        Runner.Setup(r => r.OnWeeklyGift(It.IsAny<GameTime>(), It.IsAny<ResourceType>()))
            .Callback<GameTime, ResourceType>((_, gift) =>
            {
                EventLog.Add("OnWeeklyGift");
                WeeklyGiftTypes.Add(gift);
            });
        Runner.Setup(r => r.OnPassengerWaiting(It.IsAny<GameTime>(), It.IsAny<Location>(), It.IsAny<IReadOnlyList<Passenger>>()))
            .Callback(() => EventLog.Add("OnPassengerWaiting"));
        Runner.Setup(r => r.OnStationOverrun(It.IsAny<GameTime>(), It.IsAny<Location>(), It.IsAny<IReadOnlyList<Passenger>>()))
            .Callback(() => EventLog.Add("OnStationOverrun"));
        Runner.Setup(r => r.OnGameOver(It.IsAny<GameTime>(), It.IsAny<Location>(), It.IsAny<IReadOnlyList<Passenger>>()))
            .Callback(() => EventLog.Add("OnGameOver"));
        Runner.Setup(r => r.OnDayStart(It.IsAny<GameTime>()))
            .Callback<GameTime>(t =>
            {
                DayStartCalls.Add(t);
                EventLog.Add("OnDayStart");
            });
        Runner.Setup(r => r.OnHourTick(It.IsAny<GameTime>()))
            .Callback<GameTime>(t =>
            {
                HourTickCalls.Add(t);
                EventLog.Add("OnHourTick");
            })
            .Returns(PlayerAction.None);
    }

    public Level BuildLevel() => new()
    {
        Title = "Test",
        GridWidth = 10,
        GridHeight = 10,
        LevelData = new LevelData { Seed = Seed, Stations = [.. Stations] }
    };

    /// <summary>
    /// Saves the current weekly gift sequence and resets tracking state
    /// so the simulation can run again for comparison.
    /// </summary>
    public void PrepareForRerun()
    {
        PreviousWeeklyGiftTypes.AddRange(WeeklyGiftTypes);
        WeeklyGiftTypes.Clear();
        EventLog.Clear();
        DayStartCalls.Clear();
        HourTickCalls.Clear();
        Snapshot = null;
        Result = null;
    }
}
