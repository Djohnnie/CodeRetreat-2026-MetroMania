using MetroMania.Orleans.Contracts.Models;

namespace MetroMania.Orleans.Contracts.Grains;

public interface IGameRendererGrain : IGrainWithGuidKey
{
    /// <summary>
    /// Runs the full simulation and renders a batch of frames.
    /// The grain is stateless — each call re-runs the simulation and renders only the requested range.
    /// </summary>
    Task<ScriptRenderResult> RenderBatchAsync(string base64Code, string levelDataJson, int startHour, int count);
}
