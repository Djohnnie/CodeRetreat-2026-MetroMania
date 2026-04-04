using MetroMania.Domain.Enums;
using MetroMania.Engine.Tests.Support;
using Reqnroll;

namespace MetroMania.Engine.Tests.StepDefinitions;

[Binding]
public class GameOverStepDefinitions(EngineTestContext ctx)
{
    [Then(@"OnGameOver should have fired with a (\w+) station")]
    public void ThenGameOverFiredWithStationType(string typeName)
    {
        Assert.NotEmpty(ctx.GameOverCalls);
        var expectedType = Enum.Parse<StationType>(typeName, ignoreCase: true);
        var gameOverStationId = ctx.GameOverCalls[0].StationId;
        var spawnEvent = ctx.StationSpawnedCalls.FirstOrDefault(e => e.StationId == gameOverStationId);
        Assert.NotNull(spawnEvent);
        Assert.Equal(expectedType, spawnEvent.StationType);
    }
}
