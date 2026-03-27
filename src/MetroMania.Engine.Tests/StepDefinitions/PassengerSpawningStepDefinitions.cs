using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;
using MetroMania.Engine.Model;
using MetroMania.Engine.Tests.Support;
using Reqnroll;

namespace MetroMania.Engine.Tests.StepDefinitions;

[Binding]
public class PassengerSpawningStepDefinitions(EngineTestContext ctx)
{
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
        var phases = table.Rows.Select(row => new PassengerSpawnPhase
        {
            AfterDays = int.Parse(row["AfterDays"]),
            FrequencyInHours = int.Parse(row["FrequencyInHours"])
        }).ToList();

        ctx.Stations.Add(new MetroStation
        {
            GridX = x,
            GridY = y,
            StationType = Enum.Parse<StationType>(type),
            SpawnDelayDays = delay,
            PassengerSpawnPhases = phases
        });
    }

    [Given(@"the simulation will be cancelled after (\d+) hours")]
    public void GivenTheSimulationWillBeCancelledAfterHours(int hours)
    {
        ctx.CancelAfterHours = hours;
    }

    [When(@"the simulation runs until game over or cancellation")]
    public void WhenTheSimulationRunsUntilGameOverOrCancellation()
    {
        try
        {
            var level = ctx.BuildLevel();
            ctx.Result = ctx.Engine.Run(ctx.Runner.Object, level, ctx.Cts.Token);
        }
        catch (OperationCanceledException)
        {
            ctx.WasCancelled = true;
        }
    }

    [Then(@"the simulation should have been cancelled")]
    public void ThenTheSimulationShouldHaveBeenCancelled()
    {
        Assert.True(ctx.WasCancelled, "Expected the simulation to have been cancelled");
    }

    [Then(@"the passenger waiting events should be:")]
    public void ThenThePassengerWaitingEventsShouldBe(DataTable table)
    {
        var expected = table.Rows.Select(row => new
        {
            Day = int.Parse(row["Day"]),
            Hour = int.Parse(row["Hour"]),
            X = int.Parse(row["X"]),
            Y = int.Parse(row["Y"]),
            PassengerCount = int.Parse(row["PassengerCount"])
        }).ToList();

        Assert.Equal(expected.Count, ctx.PassengerWaitingCalls.Count);

        for (int i = 0; i < expected.Count; i++)
        {
            var exp = expected[i];
            var actual = ctx.PassengerWaitingCalls[i];

            Assert.Equal(exp.Day, actual.Time.Day);
            Assert.Equal(exp.Hour, actual.Time.Hour);
            Assert.Equal(new Location(exp.X, exp.Y), actual.Location);
            Assert.Equal(exp.PassengerCount, actual.Passengers.Count);
        }
    }

    [Then(@"all passengers should have a destination type different from their origin station type")]
    public void ThenAllPassengersShouldHaveDifferentDestinationType()
    {
        Assert.NotEmpty(ctx.PassengerWaitingCalls);
        foreach (var evt in ctx.PassengerWaitingCalls)
        {
            var stationDef = ctx.Stations.First(s => s.GridX == evt.Location.X && s.GridY == evt.Location.Y);
            foreach (var passenger in evt.Passengers)
            {
                Assert.NotEqual(stationDef.StationType, passenger.DestinationType);
            }
        }
    }

    [Then(@"all passengers should have a destination type that exists among spawned station types")]
    public void ThenAllPassengersShouldHaveExistingDestinationType()
    {
        Assert.NotEmpty(ctx.PassengerWaitingCalls);
        Assert.NotNull(ctx.Snapshot);
        var spawnedTypes = ctx.Snapshot.Stations.Values.Select(s => s.Type).Distinct().ToHashSet();

        foreach (var evt in ctx.PassengerWaitingCalls)
        {
            foreach (var passenger in evt.Passengers)
            {
                Assert.Contains(passenger.DestinationType, spawnedTypes);
            }
        }
    }
}
