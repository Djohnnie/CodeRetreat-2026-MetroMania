using MetroMania.Application.Interfaces;
using MetroMania.Orleans.Client.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MetroMania.Orleans.Client;

public static class DependencyInjection
{
    public static IServiceCollection AddOrleansClient(this IServiceCollection services)
    {
        services.AddScoped<IGameEngineService, GameEngineService>();
        services.AddScoped<GameRunnerValidationService>();
        services.AddScoped<IGameRunnerValidationService>(sp => sp.GetRequiredService<GameRunnerValidationService>());
        services.AddScoped<IScriptValidationService>(sp => sp.GetRequiredService<GameRunnerValidationService>());

        return services;
    }
}
