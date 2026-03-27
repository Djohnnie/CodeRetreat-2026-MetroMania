using MetroMania.Orleans.Contracts.Grains;

namespace MetroMania.Orleans.ValidationHost.Grains;

public class GameRunnerValidationGrain : Grain, IGameRunnerValidationGrain
{
    public Task<string> PingAsync() => Task.FromResult("pong");
}
