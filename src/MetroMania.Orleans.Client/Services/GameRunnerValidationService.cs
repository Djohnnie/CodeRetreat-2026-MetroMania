using MetroMania.Orleans.Contracts.Grains;

namespace MetroMania.Orleans.Client.Services;

public class GameRunnerValidationService(IGrainFactory grainFactory) : IGameRunnerValidationService
{
    public Task<string> PingAsync(Guid grainId)
    {
        var grain = grainFactory.GetGrain<IGameRunnerValidationGrain>(grainId);
        return grain.PingAsync();
    }
}
