using MetroMania.Application.Interfaces;

namespace MetroMania.Orleans.Client.Services;

public interface IGameRunnerValidationService : IScriptValidationService
{
    Task<string> PingAsync(Guid grainId);
}
