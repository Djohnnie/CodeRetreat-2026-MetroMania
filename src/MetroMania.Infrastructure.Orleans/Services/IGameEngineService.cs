namespace MetroMania.Infrastructure.Orleans.Services;

public interface IGameEngineService
{
    Task<string> PingAsync(Guid grainId);
}
