using MetroMania.Orleans.Contracts.Grains;
using MetroMania.Orleans.Contracts.Models;

namespace MetroMania.Infrastructure.Orleans.Services;

public class GameRendererService(IGrainFactory grainFactory) : IGameRendererService
{
    public Task<ScriptRenderResult> RenderScriptAsync(Guid grainId, string base64Code, string levelDataJson)
    {
        var grain = grainFactory.GetGrain<IGameRendererGrain>(grainId);
        return grain.RenderScriptAsync(base64Code, levelDataJson);
    }
}
