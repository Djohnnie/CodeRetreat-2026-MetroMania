namespace MetroMania.Orleans.Client.Services;

public interface IGameEngineService
{
    Task<string> PingAsync(Guid grainId);
}
