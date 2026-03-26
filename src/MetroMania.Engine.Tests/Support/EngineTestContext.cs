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
    public List<(int Day, DayOfWeek DayOfWeek)> DayStartCalls { get; } = [];
    public List<(int Day, int Hour)> HourTickCalls { get; } = [];

    public EngineTestContext()
    {
        Runner = new Mock<IMetroManiaRunner>();

        Runner.Setup(r => r.OnStationSpawned(It.IsAny<Location>(), It.IsAny<StationType>()))
            .Callback(() => EventLog.Add("OnStationSpawned"));
        Runner.Setup(r => r.OnWeeklyGift(It.IsAny<ResourceType>()))
            .Callback(() => EventLog.Add("OnWeeklyGift"));
        Runner.Setup(r => r.OnPassengerWaiting(It.IsAny<Location>(), It.IsAny<IReadOnlyList<Passenger>>()))
            .Callback(() => EventLog.Add("OnPassengerWaiting"));
        Runner.Setup(r => r.OnStationOverrun(It.IsAny<Location>(), It.IsAny<IReadOnlyList<Passenger>>()))
            .Callback(() => EventLog.Add("OnStationOverrun"));
        Runner.Setup(r => r.OnGameOver(It.IsAny<Location>(), It.IsAny<IReadOnlyList<Passenger>>()))
            .Callback(() => EventLog.Add("OnGameOver"));
        Runner.Setup(r => r.OnDayStart(It.IsAny<int>(), It.IsAny<DayOfWeek>()))
            .Callback<int, DayOfWeek>((d, dow) =>
            {
                DayStartCalls.Add((d, dow));
                EventLog.Add("OnDayStart");
            });
        Runner.Setup(r => r.OnHourTick(It.IsAny<int>(), It.IsAny<int>()))
            .Callback<int, int>((d, h) =>
            {
                HourTickCalls.Add((d, h));
                EventLog.Add("OnHourTick");
            })
            .Returns(PlayerAction.None);
    }

    public Level BuildLevel() => new()
    {
        Title = "Test",
        GridWidth = 10,
        GridHeight = 10,
        LevelData = new LevelData { Seed = 42, Stations = [.. Stations] }
    };
}
