using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;
using MetroMania.Engine.Model;
using MetroMania.Engine.Tests.Support;
using Reqnroll;

namespace MetroMania.Engine.Tests.StepDefinitions;

[Binding]
public class EngineStepDefinitions(EngineTestContext ctx)
{
    // =================================================================
    // Given — level configuration
    // =================================================================

    [Given(@"an empty level$")]
    public void GivenAnEmptyLevel() { }

    [Given(@"an empty level with seed (\d+)")]
    public void GivenAnEmptyLevelWithSeed(int seed) => ctx.Seed = seed;

    [Given(@"a level with a (\w+) station at \((\d+),(\d+)\) with a spawn delay of (\d+) days?$")]
    public void GivenAStationWithDelay(string type, int x, int y, int delay)
    {
        ctx.Stations.Add(new MetroStation
        {
            GridX = x,
            GridY = y,
            StationType = Enum.Parse<StationType>(type),
            SpawnDelayDays = delay,
            PassengerSpawnPhases = []
        });
    }

    [Given(@"a level with a (\w+) station at \((\d+),(\d+)\) with a spawn delay of (\d+) days? and passengers every (\d+) hours?")]
    public void GivenAStationWithDelayAndFrequency(string type, int x, int y, int delay, int freq)
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

    [Given(@"a level with a (\w+) station at \((\d+),(\d+)\) with a spawn delay of (\d+) days? and no passenger spawn phases")]
    public void GivenAStationWithNoSpawnPhases(string type, int x, int y, int delay)
    {
        ctx.Stations.Add(new MetroStation
        {
            GridX = x,
            GridY = y,
            StationType = Enum.Parse<StationType>(type),
            SpawnDelayDays = delay,
            PassengerSpawnPhases = []
        });
    }

    [Given(@"a level with a (\w+) station at \((\d+),(\d+)\) with a spawn delay of (\d+) days? and the following spawn phases:")]
    public void GivenAStationWithSpawnPhases(string type, int x, int y, int delay, DataTable table)
    {
        var phases = table.Rows
            .Select(row => new PassengerSpawnPhase
            {
                AfterDays = int.Parse(row["AfterDays"]),
                FrequencyInHours = int.Parse(row["FrequencyInHours"])
            })
            .ToList();

        ctx.Stations.Add(new MetroStation
        {
            GridX = x,
            GridY = y,
            StationType = Enum.Parse<StationType>(type),
            SpawnDelayDays = delay,
            PassengerSpawnPhases = phases
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
                PassengerSpawnPhases = []
            });
        }
    }

    [Given(@"a weekly gift override for week (\d+) with resource type (\w+)")]
    public void GivenWeeklyGiftOverride(int week, string resourceType)
    {
        ctx.WeeklyGiftOverrides.Add(new WeeklyGiftOverride
        {
            Week = week,
            ResourceType = Enum.Parse<ResourceType>(resourceType)
        });
    }

    [Given(@"the level has (\d+) initial Lines? and (\d+) initial Trains?")]
    public void GivenInitialLinesAndTrains(int lines, int trains)
    {
        for (int i = 0; i < lines; i++) ctx.InitialResources.Add(ResourceType.Line);
        for (int i = 0; i < trains; i++) ctx.InitialResources.Add(ResourceType.Train);
    }

    [Given(@"the level has (\d+) initial Lines?$")]
    public void GivenInitialLines(int lines)
    {
        for (int i = 0; i < lines; i++) ctx.InitialResources.Add(ResourceType.Line);
    }

    [Given(@"the level has (\d+) initial Trains?$")]
    public void GivenInitialTrains(int trains)
    {
        for (int i = 0; i < trains; i++) ctx.InitialResources.Add(ResourceType.Train);
    }

    [Given(@"the vehicle capacity is (\d+)")]
    public void GivenVehicleCapacity(int capacity) => ctx.VehicleCapacity = capacity;

    // =================================================================
    // When — run simulation
    // =================================================================

    [When(@"the simulation runs for (\d+) hours?")]
    public void WhenTheSimulationRunsForHours(int hours)
    {
        ctx.SimResult = ctx.Engine.RunSimulation(ctx.Runner.Object, ctx.BuildLevel(), hours);
    }

    [When(@"the simulation runs again for (\d+) hours with the same seed")]
    public void WhenTheSimulationRunsAgainForHoursWithTheSameSeed(int hours)
    {
        ctx.PrepareForRerun();
        ctx.SimResult = ctx.Engine.RunSimulation(ctx.Runner.Object, ctx.BuildLevel(), hours);
    }

    // =================================================================
    // Then — event count assertions
    // =================================================================

    [Then(@"""(.*)"" should have fired (\d+) times?")]
    public void ThenEventFiredNTimes(string eventName, int expected)
    {
        int actual = ctx.EventLog.Count(e => e == eventName);
        Assert.Equal(expected, actual);
    }

    [Then(@"""(.*)"" should have fired exactly (\d+) times?")]
    public void ThenEventFiredExactlyNTimes(string eventName, int expected)
    {
        int actual = ctx.EventLog.Count(e => e == eventName);
        Assert.Equal(expected, actual);
    }

    // =================================================================
    // Then — event ordering assertions
    // =================================================================

    [Then(@"""(.*)"" should have fired before ""(.*)""")]
    public void ThenEventFiredBefore(string first, string second)
    {
        int firstIdx = ctx.EventLog.IndexOf(first);
        int secondIdx = ctx.EventLog.LastIndexOf(second);
        Assert.True(firstIdx >= 0, $"Expected '{first}' to have fired");
        Assert.True(secondIdx >= 0, $"Expected '{second}' to have fired");
        Assert.True(firstIdx < secondIdx,
            $"Expected '{first}' (at index {firstIdx}) to fire before '{second}' (at index {secondIdx})");
    }

    [Then(@"""(.*)"" should be the last event fired")]
    public void ThenEventShouldBeTheLastFired(string eventName)
    {
        Assert.NotEmpty(ctx.EventLog);
        Assert.Equal(eventName, ctx.EventLog[^1]);
    }

    [Then(@"the event log should be ""([^""]+)"", ""([^""]+)""")]
    public void ThenEventLogShouldBeTwoEvents(string e1, string e2)
    {
        Assert.Equal([e1, e2], ctx.EventLog);
    }

    [Then(@"the event log should be ""([^""]+)"", ""([^""]+)"", ""([^""]+)""")]
    public void ThenEventLogShouldBeThreeEvents(string e1, string e2, string e3)
    {
        Assert.Equal([e1, e2, e3], ctx.EventLog);
    }

    [Then(@"the event log should start with ""([^""]+)"", ""([^""]+)"", ""([^""]+)""")]
    public void ThenEventLogStartsWithThreeEvents(string e1, string e2, string e3)
    {
        Assert.True(ctx.EventLog.Count >= 3,
            $"Expected at least 3 events but got {ctx.EventLog.Count}");
        Assert.Equal(e1, ctx.EventLog[0]);
        Assert.Equal(e2, ctx.EventLog[1]);
        Assert.Equal(e3, ctx.EventLog[2]);
    }

    [Then(@"the last (\d+) events should be ""([^""]+)"", ""([^""]+)"", ""([^""]+)""")]
    public void ThenLastThreeEventsShouldBe(int count, string e1, string e2, string e3)
    {
        Assert.True(ctx.EventLog.Count >= count,
            $"Expected at least {count} events but got {ctx.EventLog.Count}");
        int offset = ctx.EventLog.Count - count;
        Assert.Equal(e1, ctx.EventLog[offset]);
        Assert.Equal(e2, ctx.EventLog[offset + 1]);
        Assert.Equal(e3, ctx.EventLog[offset + 2]);
    }

    [Then(@"the last (\d+) events should be ""([^""]+)"", ""([^""]+)"", ""([^""]+)"", ""([^""]+)""")]
    public void ThenLastFourEventsShouldBe(int count, string e1, string e2, string e3, string e4)
    {
        Assert.True(ctx.EventLog.Count >= count,
            $"Expected at least {count} events but got {ctx.EventLog.Count}");
        int offset = ctx.EventLog.Count - count;
        Assert.Equal(e1, ctx.EventLog[offset]);
        Assert.Equal(e2, ctx.EventLog[offset + 1]);
        Assert.Equal(e3, ctx.EventLog[offset + 2]);
        Assert.Equal(e4, ctx.EventLog[offset + 3]);
    }

    [Then(@"the last (\d+) events should be ""([^""]+)"", ""([^""]+)"", ""([^""]+)"", ""([^""]+)"", ""([^""]+)""")]
    public void ThenLastFiveEventsShouldBe(int count, string e1, string e2, string e3, string e4, string e5)
    {
        Assert.True(ctx.EventLog.Count >= count,
            $"Expected at least {count} events but got {ctx.EventLog.Count}");
        int offset = ctx.EventLog.Count - count;
        Assert.Equal(e1, ctx.EventLog[offset]);
        Assert.Equal(e2, ctx.EventLog[offset + 1]);
        Assert.Equal(e3, ctx.EventLog[offset + 2]);
        Assert.Equal(e4, ctx.EventLog[offset + 3]);
        Assert.Equal(e5, ctx.EventLog[offset + 4]);
    }

    // =================================================================
    // Then — game time assertions
    // =================================================================

    [Then(@"the last tick should report day (\d+) and hour (\d+)")]
    public void ThenLastTickReportsDayAndHour(int day, int hour)
    {
        Assert.NotEmpty(ctx.HourTickCalls);
        var last = ctx.HourTickCalls[^1];
        Assert.Equal(day, last.Day);
        Assert.Equal(hour, last.Hour);
    }

    [Then(@"the last snapshot TotalHoursElapsed should be (\d+)")]
    public void ThenLastSnapshotTotalHoursElapsed(int expected)
    {
        Assert.NotNull(ctx.LastSnapshot);
        Assert.Equal(expected, ctx.LastSnapshot.TotalHoursElapsed);
    }

    [Then(@"the last tick DayOfWeek should be (\w+)")]
    public void ThenLastTickDayOfWeek(string dayOfWeekName)
    {
        Assert.NotEmpty(ctx.HourTickCalls);
        var expected = Enum.Parse<DayOfWeek>(dayOfWeekName);
        Assert.Equal(expected, ctx.HourTickCalls[^1].DayOfWeek);
    }

    [Then(@"all tick DayOfWeek values should follow the Sunday-starting cycle")]
    public void ThenAllTickDayOfWeekValuesFollowCycle()
    {
        Assert.NotEmpty(ctx.HourTickCalls);
        foreach (var tick in ctx.HourTickCalls)
        {
            // Day 1 = Sunday (DayOfWeek 0), day 2 = Monday (1), etc.
            var expected = (DayOfWeek)((tick.Day - 1) % 7);
            Assert.Equal(expected, tick.DayOfWeek);
        }
    }

    // =================================================================
    // Then — simulation state assertions
    // =================================================================

    [Then(@"the score should be (\d+)")]
    public void ThenScoreShouldBe(int expected)
    {
        Assert.NotNull(ctx.LastSnapshot);
        Assert.Equal(expected, ctx.LastSnapshot.Score);
    }

    [Then(@"there should be (\d+) trains? in the simulation")]
    public void ThenTrainCount(int expected)
    {
        Assert.NotNull(ctx.LastSnapshot);
        Assert.Equal(expected, ctx.LastSnapshot.Trains.Count);
    }

    [Then(@"there should be (\d+) lines? in the simulation")]
    public void ThenLineCount(int expected)
    {
        Assert.NotNull(ctx.LastSnapshot);
        Assert.Equal(expected, ctx.LastSnapshot.Lines.Count);
    }

    [Then(@"the simulation should have produced (\d+) snapshots?")]
    public void ThenSimulationSnapshotCount(int expected)
    {
        Assert.NotNull(ctx.SimResult);
        Assert.Equal(expected, ctx.SimResult.GameSnapshots.Count);
    }

    [Then(@"the simulation should have produced fewer than (\d+) snapshots?")]
    public void ThenSimulationSnapshotCountLessThan(int max)
    {
        Assert.NotNull(ctx.SimResult);
        Assert.True(ctx.SimResult.GameSnapshots.Count < max,
            $"Expected fewer than {max} snapshots but got {ctx.SimResult.GameSnapshots.Count}");
    }

    [Then(@"the last snapshot should contain (\d+) resources?")]
    public void ThenLastSnapshotResourceCount(int expected)
    {
        Assert.NotNull(ctx.LastSnapshot);
        Assert.Equal(expected, ctx.LastSnapshot.Resources.Count);
    }
}
