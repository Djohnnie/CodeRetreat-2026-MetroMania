using MetroMania.Orleans.Contracts.Grains;
using MetroMania.Orleans.Contracts.Models;

namespace MetroMania.Infrastructure.Orleans.Services;

public class GameRendererService(IGrainFactory grainFactory) : IGameRendererService
{
    public Task<ScriptPrepareResult> PrepareRenderAsync(Guid grainId, string base64Code, string levelDataJson)
    {
        var grain = grainFactory.GetGrain<IGameRendererGrain>(grainId);
        return grain.PrepareAsync(base64Code, levelDataJson);
    }

    public Task<ScriptRenderResult> RenderBatchAsync(Guid grainId, int startHour, int count)
    {
        var grain = grainFactory.GetGrain<IGameRendererGrain>(grainId);
        return grain.RenderBatchAsync(startHour, count);
    }
}
