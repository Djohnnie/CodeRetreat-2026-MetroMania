using MetroMania.Orleans.Contracts.Models;

namespace MetroMania.Infrastructure.Orleans.Services;

public interface IGameRendererService
{
    Task<ScriptRenderResult> RenderScriptAsync(Guid grainId, string base64Code, string levelDataJson);
}
