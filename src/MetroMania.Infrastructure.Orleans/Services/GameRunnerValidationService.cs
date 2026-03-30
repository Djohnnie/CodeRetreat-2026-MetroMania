using MetroMania.Application.Interfaces;
using MetroMania.Orleans.Contracts.Grains;

namespace MetroMania.Infrastructure.Orleans.Services;

public class GameRunnerValidationService(IGrainFactory grainFactory) : IGameRunnerValidationService
{
    public Task<string> PingAsync(Guid grainId)
    {
        var grain = grainFactory.GetGrain<IGameRunnerValidationGrain>(grainId);
        return grain.PingAsync();
    }

    public async Task<ScriptValidationResult> ValidateAsync(string base64Code)
    {
        var grain = grainFactory.GetGrain<IGameRunnerValidationGrain>(Guid.NewGuid());
        var result = await grain.ValidateScriptAsync(base64Code);

        return new ScriptValidationResult(result.Success, result.Errors);
    }
}
