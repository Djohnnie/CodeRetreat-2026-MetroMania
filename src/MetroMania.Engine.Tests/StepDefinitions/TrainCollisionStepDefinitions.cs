using MetroMania.Engine.Tests.Support;
using Reqnroll;

namespace MetroMania.Engine.Tests.StepDefinitions;

[Binding]
public class TrainCollisionStepDefinitions(EngineTestContext ctx)
{
    [Then(@"OnInvalidPlayerAction should have fired with code (\d+)")]
    public void ThenInvalidPlayerActionCode(int code)
    {
        Assert.NotEmpty(ctx.InvalidActionCalls);
        Assert.Contains(ctx.InvalidActionCalls, e => e.Code == code);
    }
}
