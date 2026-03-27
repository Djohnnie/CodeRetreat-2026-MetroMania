namespace MetroMania.Orleans.Client.Services;

public interface IGameRunnerValidationService
{
    Task<string> PingAsync(Guid grainId);
}
