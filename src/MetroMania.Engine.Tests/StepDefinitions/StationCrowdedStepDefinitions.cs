using MetroMania.Domain.Enums;
using MetroMania.Engine.Tests.Support;
using Reqnroll;

namespace MetroMania.Engine.Tests.StepDefinitions;

[Binding]
public class StationCrowdedStepDefinitions(EngineTestContext ctx)
{
    [Then(@"OnStationCrowded should have fired with a (\w+) station")]
    public void ThenStationCrowdedFiredWithStationType(string typeName)
    {
        Assert.NotEmpty(ctx.OverrunCalls);
        var expectedType = Enum.Parse<StationType>(typeName, ignoreCase: true);
        var crowdedStationId = ctx.OverrunCalls[0].StationId;
        var spawnEvent = ctx.StationSpawnedCalls.FirstOrDefault(e => e.StationId == crowdedStationId);
        Assert.NotNull(spawnEvent);
        Assert.Equal(expectedType, spawnEvent.StationType);
    }
}
