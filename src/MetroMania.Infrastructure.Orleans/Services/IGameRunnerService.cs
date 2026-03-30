using MetroMania.Orleans.Contracts.Grains;
using MetroMania.Orleans.Contracts.Models;

namespace MetroMania.Infrastructure.Orleans.Services;

public interface IGameRunnerService
{
    Task<string> PingAsync(Guid grainId);
    Task<ScriptRunResult> RunScriptAsync(Guid grainId, string base64Code, string levelDataJson);
}
