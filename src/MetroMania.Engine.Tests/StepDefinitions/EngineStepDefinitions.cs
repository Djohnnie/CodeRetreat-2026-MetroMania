using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;
using MetroMania.Engine.Model;
using MetroMania.Engine.Tests.Support;
using Moq;
using Reqnroll;

namespace MetroMania.Engine.Tests.StepDefinitions;

[Binding]
public class EngineStepDefinitions(EngineTestContext ctx)
{
    // =================================================================
    // Given
    // =================================================================

    [Given(@"a level with a (\w+) station at \((\d+),(\d+)\) with a spawn delay of (\d+) days?")]
    public void GivenAStationWithDelay(string type, int x, int y, int delay)
    {
        ctx.Stations.Add(new MetroStation
        {
            GridX = x,
            GridY = y,
            StationType = Enum.Parse<StationType>(type),
            SpawnDelayDays = delay,
            PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 999 }]
        });
    }

    [Given(@"a level with a (\w+) station at \((\d+),(\d+)\) with a spawn delay of (\d+) days? and passengers every (\d+) hours?")]
    public void GivenAStationWithDelayAndPassengers(string type, int x, int y, int delay, int freq)
    {
        ctx.Stations.Add(new MetroStation
        {
            GridX = x,
            GridY = y,
            StationType = Enum.Parse<StationType>(type),
            SpawnDelayDays = delay,
            PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = freq }]
        });
    }

    [Given(@"a level with the following stations:")]
    public void GivenALevelWithTheFollowingStations(DataTable table)
    {
        foreach (var row in table.Rows)
        {
            ctx.Stations.Add(new MetroStation
            {
                GridX = int.Parse(row["X"]),
                GridY = int.Parse(row["Y"]),
                StationType = Enum.Parse<StationType>(row["Type"]),
                SpawnDelayDays = int.Parse(row["SpawnDelay"]),
                PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 999 }]
            });
        }
    }

    [Given(@"an empty level")]
    public void GivenAnEmptyLevel()
    {
        // No stations — ctx.Stations is already empty
    }

    // =================================================================
    // When
    // =================================================================

    [When(@"the simulation runs for (\d+) hours?")]
    public void WhenTheSimulationRunsForHours(int hours)
    {
        var level = ctx.BuildLevel();
        ctx.Snapshot = ctx.Engine.RunForHours(ctx.Runner.Object, level, hours);
    }

    [When(@"the simulation runs until game over")]
    public void WhenTheSimulationRunsUntilGameOver()
    {
        var level = ctx.BuildLevel();
        ctx.Result = ctx.Engine.Run(ctx.Runner.Object, level);
    }

    // =================================================================
    // Then — Station Spawning
    // =================================================================

    [Then(@"the (\w+) station at \((\d+),(\d+)\) should have spawned")]
    public void ThenStationShouldHaveSpawned(string type, int x, int y)
    {
        ctx.Runner.Verify(
            r => r.OnStationSpawned(It.IsAny<GameSnapshot>(), It.IsAny<Guid>(), new Location(x, y), Enum.Parse<StationType>(type)),
            Times.Once);
    }

    [Then(@"the (\w+) station at \((\d+),(\d+)\) should not have spawned")]
    public void ThenStationShouldNotHaveSpawned(string type, int x, int y)
    {
        ctx.Runner.Verify(
            r => r.OnStationSpawned(It.IsAny<GameSnapshot>(), It.IsAny<Guid>(), new Location(x, y), Enum.Parse<StationType>(type)),
            Times.Never);
    }

    [Then(@"the game snapshot should contain a (\w+) station at \((\d+),(\d+)\)")]
    public void ThenSnapshotContainsStation(string type, int x, int y)
    {
        Assert.NotNull(ctx.Snapshot);
        var location = new Location(x, y);
        Assert.True(ctx.Snapshot.Stations.ContainsKey(location));
        Assert.Equal(Enum.Parse<StationType>(type), ctx.Snapshot.Stations[location].Type);
    }

    [Then(@"the game snapshot should contain no stations")]
    public void ThenSnapshotContainsNoStations()
    {
        Assert.NotNull(ctx.Snapshot);
        Assert.Empty(ctx.Snapshot.Stations);
    }

    // =================================================================
    // Then — Event Ordering
    // =================================================================

    [Then(@"""(.*)"" should have fired before ""(.*)""")]
    public void ThenEventShouldHaveFiredBefore(string first, string second)
    {
        int firstIdx = ctx.EventLog.IndexOf(first);
        int secondIdx = ctx.EventLog.IndexOf(second);

        Assert.True(firstIdx >= 0, $"{first} should have fired");
        Assert.True(secondIdx >= 0, $"{second} should have fired");
        Assert.True(firstIdx < secondIdx, $"{first} should fire before {second}");
    }

    [Then(@"""(.*)"" should have fired exactly (\d+) times?")]
    public void ThenEventShouldHaveFiredExactlyNTimes(string eventName, int count)
    {
        int actual = ctx.EventLog.Count(e => e == eventName);
        Assert.Equal(count, actual);
    }

    [Then(@"""(.*)"" should have fired (\d+) times")]
    public void ThenEventShouldHaveFiredNTimes(string eventName, int count)
    {
        int actual = ctx.EventLog.Count(e => e == eventName);
        Assert.Equal(count, actual);
    }

    [Then(@"""OnDayStart"" should have fired for days (\d+) and (\d+)")]
    public void ThenOnDayStartFiredForDays(int day1, int day2)
    {
        Assert.Equal(day1, ctx.DayStartCalls[0].Day);
        Assert.Equal(day2, ctx.DayStartCalls[1].Day);
    }

    [Then(@"on each day boundary ""OnDayStart"" should fire directly before ""OnHourTick""")]
    public void ThenOnEachDayBoundaryOnDayStartDirectlyBeforeOnHourTick()
    {
        var dayStartIndices = ctx.EventLog
            .Select((e, i) => (e, i))
            .Where(x => x.e == "OnDayStart")
            .Select(x => x.i)
            .ToList();

        Assert.NotEmpty(dayStartIndices);

        foreach (var dsIdx in dayStartIndices)
        {
            Assert.True(dsIdx + 1 < ctx.EventLog.Count, "OnHourTick should follow OnDayStart");
            Assert.Equal("OnHourTick", ctx.EventLog[dsIdx + 1]);
        }
    }

    [Then(@"each hour tick should report the correct day and hour")]
    public void ThenEachHourTickShouldReportCorrectDayAndHour()
    {
        for (int i = 0; i < ctx.HourTickCalls.Count; i++)
        {
            int expectedDay = i / 24 + 1;
            int expectedHour = i % 24;
            Assert.Equal(expectedDay, ctx.HourTickCalls[i].Day);
            Assert.Equal(expectedHour, ctx.HourTickCalls[i].Hour);
        }
    }

    [Then(@"the first ""(.*)"" should appear before the second ""(.*)""")]
    public void ThenFirstEventBeforeSecondOccurrenceOfAnother(string first, string second)
    {
        int firstIdx = ctx.EventLog.IndexOf(first);
        int secondIdx = ctx.EventLog
            .Select((e, i) => (e, i))
            .Where(x => x.e == second)
            .Skip(1)
            .First().i;

        Assert.True(firstIdx >= 0, $"{first} should have fired");
        Assert.True(firstIdx < secondIdx, $"{first} should appear before the second {second}");
    }

    [Then(@"""(.*)"" should have fired directly before ""(.*)""")]
    public void ThenEventShouldHaveFiredDirectlyBefore(string first, string second)
    {
        int firstIdx = ctx.EventLog.IndexOf(first);
        Assert.True(firstIdx >= 0, $"{first} should have fired");
        Assert.True(firstIdx + 1 < ctx.EventLog.Count, $"An event should follow {first}");
        Assert.Equal(second, ctx.EventLog[firstIdx + 1]);
    }

    [Then(@"""(.*)"" should be the last event fired")]
    public void ThenEventShouldBeTheLastFired(string eventName)
    {
        Assert.NotEmpty(ctx.EventLog);
        Assert.Equal(eventName, ctx.EventLog[^1]);
    }

    [Then(@"the first (\d+) events should be ""(.*)"", ""(.*)"", ""(.*)"", ""(.*)""")]
    public void ThenFirstFourEventsShouldBe(int count, string e1, string e2, string e3, string e4)
    {
        Assert.True(ctx.EventLog.Count >= count);
        Assert.Equal(e1, ctx.EventLog[0]);
        Assert.Equal(e2, ctx.EventLog[1]);
        Assert.Equal(e3, ctx.EventLog[2]);
        Assert.Equal(e4, ctx.EventLog[3]);
    }

    // =================================================================
    // Then — Game Simulation
    // =================================================================

    [Then(@"the snapshot should show day (\d+) and hour (\d+)")]
    public void ThenSnapshotShowsDayAndHour(int day, int hour)
    {
        Assert.NotNull(ctx.Snapshot);
        Assert.Equal(day, ctx.Snapshot.Time.Day);
        Assert.Equal(hour, ctx.Snapshot.Time.Hour);
    }

    [Then(@"the snapshot should show (\d+) total hours elapsed")]
    public void ThenSnapshotShowsTotalHours(int hours)
    {
        Assert.NotNull(ctx.Snapshot);
        Assert.Equal(hours, ctx.Snapshot.TotalHoursElapsed);
    }

    [Then(@"the game should not be over")]
    public void ThenGameShouldNotBeOver()
    {
        Assert.NotNull(ctx.Snapshot);
        Assert.False(ctx.Snapshot.GameOver);
    }

    [Then(@"the score should be greater than (\d+)")]
    public void ThenScoreShouldBeGreaterThan(int minimum)
    {
        Assert.NotNull(ctx.Result);
        Assert.True(ctx.Result.Score > minimum);
    }

    [Then(@"at least (\d+) passengers should have been spawned")]
    public void ThenAtLeastNPassengersSpawned(int minimum)
    {
        Assert.NotNull(ctx.Result);
        Assert.True(ctx.Result.TotalPassengersSpawned >= minimum);
    }

    [Then(@"all spawned stations should have unique Ids")]
    public void ThenAllSpawnedStationsShouldHaveUniqueIds()
    {
        Assert.NotNull(ctx.Snapshot);
        var ids = ctx.Snapshot.Stations.Values.Select(s => s.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Then(@"the DayOfWeek values should cycle Monday through Sunday correctly")]
    public void ThenDayOfWeekValuesShouldCycleCorrectly()
    {
        Assert.NotEmpty(ctx.HourTickCalls);
        foreach (var tick in ctx.HourTickCalls)
        {
            // day 1 = Monday (1%7=1), day 7 = Sunday (7%7=0), day 8 = Monday (8%7=1)
            var expectedDow = (DayOfWeek)(tick.Day % 7);
            Assert.Equal(expectedDow, tick.DayOfWeek);
        }
    }
}
