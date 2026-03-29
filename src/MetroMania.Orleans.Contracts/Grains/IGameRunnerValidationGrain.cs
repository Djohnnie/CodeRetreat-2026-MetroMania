using MetroMania.Orleans.Contracts.Models;

namespace MetroMania.Orleans.Contracts.Grains;

public interface IGameRunnerValidationGrain : IGrainWithGuidKey
{
    Task<string> PingAsync();
    Task<ScriptValidationResult> ValidateScriptAsync(string base64Code);
}
