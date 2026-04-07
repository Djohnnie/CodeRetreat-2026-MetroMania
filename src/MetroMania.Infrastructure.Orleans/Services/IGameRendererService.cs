using MetroMania.Orleans.Contracts.Models;

namespace MetroMania.Infrastructure.Orleans.Services;

public interface IGameRendererService
{
    Task<ScriptRenderResult> RenderBatchAsync(Guid grainId, string base64Code, string levelDataJson, int startHour, int count);
}
