using MetroMania.Orleans.Contracts.Grains;
using MetroMania.Orleans.Contracts.Models;

namespace MetroMania.Infrastructure.Orleans.Services;

public class GameRunnerService(IGrainFactory grainFactory) : IGameRunnerService
{
    public Task<string> PingAsync(Guid grainId)
    {
        var grain = grainFactory.GetGrain<IGameRunnerGrain>(grainId);
        return grain.PingAsync();
    }

    public Task<ScriptRunResult> RunScriptAsync(Guid grainId, string base64Code, string levelDataJson)
    {
        var grain = grainFactory.GetGrain<IGameRunnerGrain>(grainId);
        return grain.RunScriptAsync(base64Code, levelDataJson);
    }
}
