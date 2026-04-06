using MetroMania.Engine.Model;
using MetroMania.Engine.Tests.Support;
using Reqnroll;

namespace MetroMania.Engine.Tests.StepDefinitions;

/// <summary>
/// Step definitions for asserting GameSnapshot properties, simulation results,
/// and player action recording.
/// </summary>
[Binding]
public class GameSnapshotPropertyStepDefinitions(EngineTestContext ctx)
{
    [Then(@"the last snapshot LastAction should be NoAction")]
    public void ThenLastActionIsNoAction()
    {
        Assert.NotNull(ctx.LastSnapshot);
        Assert.IsType<NoAction>(ctx.LastSnapshot.LastAction);
    }

    [Then(@"the last snapshot LastAction should be CreateLine")]
    public void ThenLastActionIsCreateLine()
    {
        Assert.NotNull(ctx.LastSnapshot);
        Assert.IsType<CreateLine>(ctx.LastSnapshot.LastAction);
    }

    [Then(@"the last snapshot LastAction should be ExtendLineFromTerminal")]
    public void ThenLastActionIsExtendLineFromTerminal()
    {
        Assert.NotNull(ctx.LastSnapshot);
        Assert.IsType<ExtendLineFromTerminal>(ctx.LastSnapshot.LastAction);
    }

    [Then(@"the last snapshot should have day (\d+) and hour (\d+)")]
    public void ThenLastSnapshotTime(int day, int hour)
    {
        Assert.NotNull(ctx.LastSnapshot);
        Assert.Equal(day, ctx.LastSnapshot.Time.Day);
        Assert.Equal(hour, ctx.LastSnapshot.Time.Hour);
    }

    [Then(@"the first train should have direction (\-?\d+)")]
    public void ThenFirstTrainDirection(int expectedDir)
    {
        Assert.NotNull(ctx.LastSnapshot);
        Assert.NotEmpty(ctx.LastSnapshot.Trains);
        Assert.Equal(expectedDir, ctx.LastSnapshot.Trains[0].Direction);
    }

    [Then(@"snapshot at index (\d+) should have score (\d+)")]
    public void ThenSnapshotAtIndexScore(int index, int expectedScore)
    {
        Assert.NotNull(ctx.SimResult);
        Assert.True(ctx.SimResult.GameSnapshots.Count > index,
            $"Expected at least {index + 1} snapshots but got {ctx.SimResult.GameSnapshots.Count}");
        Assert.Equal(expectedScore, ctx.SimResult.GameSnapshots[index].Score);
    }

    [Then(@"the simulation result TotalScore should equal the last snapshot score")]
    public void ThenTotalScoreEqualsLastSnapshotScore()
    {
        Assert.NotNull(ctx.SimResult);
        Assert.NotNull(ctx.LastSnapshot);
        Assert.Equal(ctx.LastSnapshot.Score, ctx.SimResult.TotalScore);
    }

    [Then(@"the simulation result DaysSurvived should be (\d+)")]
    public void ThenDaysSurvived(int expected)
    {
        Assert.NotNull(ctx.SimResult);
        Assert.Equal(expected, ctx.SimResult.DaysSurvived);
    }

    [Then(@"the simulation result NumberOfPlayerActions should be (\d+)")]
    public void ThenNumberOfPlayerActions(int expected)
    {
        Assert.NotNull(ctx.SimResult);
        Assert.Equal(expected, ctx.SimResult.NumberOfPlayerActions);
    }

    [Then(@"all snapshots should have sequential TotalHoursElapsed values starting at 0")]
    public void ThenAllSnapshotsSequentialHours()
    {
        Assert.NotNull(ctx.SimResult);
        for (int i = 0; i < ctx.SimResult.GameSnapshots.Count; i++)
            Assert.Equal(i, ctx.SimResult.GameSnapshots[i].TotalHoursElapsed);
    }

    [Then(@"the game over snapshot should be included in the snapshot history")]
    public void ThenGameOverSnapshotInHistory()
    {
        Assert.NotNull(ctx.SimResult);
        Assert.True(ctx.GameOverCalls.Count > 0, "Game over should have fired");
        Assert.True(ctx.SimResult.GameSnapshots.Count > 0, "At least one snapshot should exist");
    }

    [Then(@"the first passenger on the first train should have SpawnedAtHour (\d+)")]
    public void ThenFirstPassengerSpawnedAtHour(int expected)
    {
        Assert.NotNull(ctx.LastSnapshot);
        Assert.NotEmpty(ctx.LastSnapshot.Trains);
        Assert.NotEmpty(ctx.LastSnapshot.Trains[0].Passengers);
        Assert.Equal(expected, ctx.LastSnapshot.Trains[0].Passengers[0].SpawnedAtHour);
    }

    [Then(@"both runs should have produced the same number of passenger spawn events")]
    public void ThenBothRunsSamePassengerSpawnCount()
    {
        Assert.Equal(ctx.PreviousPassengerSpawnCount, ctx.PassengerSpawnedCalls.Count);
    }
}
