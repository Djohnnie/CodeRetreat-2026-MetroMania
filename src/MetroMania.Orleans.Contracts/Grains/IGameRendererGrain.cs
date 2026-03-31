using MetroMania.Orleans.Contracts.Models;

namespace MetroMania.Orleans.Contracts.Grains;

public interface IGameRendererGrain : IGrainWithGuidKey
{
    Task<ScriptRenderResult> RenderScriptAsync(string base64Code, string levelDataJson);
}
