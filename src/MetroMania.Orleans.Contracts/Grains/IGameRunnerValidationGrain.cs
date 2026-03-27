namespace MetroMania.Orleans.Contracts.Grains;

public interface IGameRunnerValidationGrain : IGrainWithGuidKey
{
    Task<string> PingAsync();
}
