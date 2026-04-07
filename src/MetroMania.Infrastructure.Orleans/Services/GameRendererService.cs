using MetroMania.Orleans.Contracts.Grains;
using MetroMania.Orleans.Contracts.Models;

namespace MetroMania.Infrastructure.Orleans.Services;

public class GameRendererService(IGrainFactory grainFactory) : IGameRendererService
{
    public Task<ScriptRenderResult> RenderBatchAsync(Guid grainId, string base64Code, string levelDataJson, int startHour, int count)
    {
        var grain = grainFactory.GetGrain<IGameRendererGrain>(grainId);
        return grain.RenderBatchAsync(base64Code, levelDataJson, startHour, count);
    }
}
