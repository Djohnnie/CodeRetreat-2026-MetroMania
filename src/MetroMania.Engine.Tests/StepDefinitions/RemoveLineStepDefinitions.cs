using MetroMania.Engine.Model;
using MetroMania.Engine.Tests.Support;
using Reqnroll;

namespace MetroMania.Engine.Tests.StepDefinitions;

/// <summary>
/// Step definitions for the RemoveLine feature.
/// Covers line-specific assertions (pending removal state) and action steps
/// that are unique to the RemoveLine scenarios (e.g. recreating a line after removal).
/// </summary>
[Binding]
public class RemoveLineStepDefinitions(EngineTestContext ctx)
{
    [Then(@"the first line should have pending removal")]
    public void ThenTheFirstLineShouldHavePendingRemoval()
    {
        var line = ctx.LastSnapshot!.Lines.FirstOrDefault();
        Assert.NotNull(line);
        Assert.True(line.PendingRemoval, "Expected the first line to have PendingRemoval = true");
    }

    /// <summary>
    /// Queues a CreateLine action that reuses the first available (non-in-use) Line resource.
    /// Used to verify that a line resource released by RemoveLine can be redeployed.
    /// </summary>
    [Given(@"the runner will recreate a line between stations at \((\d+),(\d+)\) and \((\d+),(\d+)\)")]
    public void GivenTheRunnerWillRecreateALineBetweenStations(int x1, int y1, int x2, int y2)
    {
        ctx.PendingActions.Enqueue(snapshot =>
        {
            var lineResource = snapshot.Resources
                .FirstOrDefault(r => r.Type == Domain.Enums.ResourceType.Line && !r.InUse);
            if (lineResource is null) return PlayerAction.None;
            var stationA = snapshot.Stations[new Location(x1, y1)];
            var stationB = snapshot.Stations[new Location(x2, y2)];
            return new CreateLine(lineResource.Id, stationA.Id, stationB.Id);
        });
    }
}
