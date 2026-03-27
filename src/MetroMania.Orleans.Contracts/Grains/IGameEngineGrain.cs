namespace MetroMania.Orleans.Contracts.Grains;

public interface IGameEngineGrain : IGrainWithGuidKey
{
    Task<string> PingAsync();
}
