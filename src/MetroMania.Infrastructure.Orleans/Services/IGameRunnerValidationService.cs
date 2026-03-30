using MetroMania.Application.Interfaces;

namespace MetroMania.Infrastructure.Orleans.Services;

public interface IGameRunnerValidationService : IScriptValidationService
{
    Task<string> PingAsync(Guid grainId);
}
