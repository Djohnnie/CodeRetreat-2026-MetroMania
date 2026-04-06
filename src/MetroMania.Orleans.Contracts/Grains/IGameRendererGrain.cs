using MetroMania.Orleans.Contracts.Models;

namespace MetroMania.Orleans.Contracts.Grains;

public interface IGameRendererGrain : IGrainWithGuidKey
{
    /// <summary>
    /// Runs the simulation and stores snapshots in grain state.
    /// Returns the total number of frames available for rendering.
    /// </summary>
    Task<ScriptPrepareResult> PrepareAsync(string base64Code, string levelDataJson);

    /// <summary>
    /// Renders a batch of frames from the stored snapshots.
    /// Must call <see cref="PrepareAsync"/> first.
    /// The grain deactivates automatically after the last batch is rendered.
    /// </summary>
    Task<ScriptRenderResult> RenderBatchAsync(int startHour, int count);
}
