using MetroMania.Orleans.Contracts.Models;

namespace MetroMania.Orleans.Contracts.Grains;

public interface IGameRunnerGrain : IGrainWithGuidKey
{
    Task<string> PingAsync();
    Task<ScriptRunResult> RunScriptAsync(string base64Code, string levelDataJson);
}
