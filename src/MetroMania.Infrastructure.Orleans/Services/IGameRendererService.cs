using MetroMania.Orleans.Contracts.Models;

namespace MetroMania.Infrastructure.Orleans.Services;

public interface IGameRendererService
{
    Task<ScriptPrepareResult> PrepareRenderAsync(Guid grainId, string base64Code, string levelDataJson);
    Task<ScriptRenderResult> RenderBatchAsync(Guid grainId, int startHour, int count);
}
