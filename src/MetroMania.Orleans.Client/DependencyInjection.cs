using MetroMania.Orleans.Client.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MetroMania.Orleans.Client;

public static class DependencyInjection
{
    public static IServiceCollection AddOrleansClient(this IServiceCollection services)
    {
        services.AddScoped<IGameEngineService, GameEngineService>();
        services.AddScoped<IGameRunnerValidationService, GameRunnerValidationService>();

        return services;
    }
}
