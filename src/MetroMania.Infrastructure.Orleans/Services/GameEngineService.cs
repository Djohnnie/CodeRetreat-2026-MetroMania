using MetroMania.Orleans.Contracts.Grains;

namespace MetroMania.Infrastructure.Orleans.Services;

public class GameEngineService(IGrainFactory grainFactory) : IGameEngineService
{
    public Task<string> PingAsync(Guid grainId)
    {
        var grain = grainFactory.GetGrain<IGameEngineGrain>(grainId);
        return grain.PingAsync();
    }
}
