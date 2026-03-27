using MetroMania.Orleans.Contracts.Grains;

namespace MetroMania.Orleans.Host.Grains;

public class GameEngineGrain : Grain, IGameEngineGrain
{
    public Task<string> PingAsync() => Task.FromResult("pong");
}
