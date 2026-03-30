using MetroMania.Application.Interfaces;
using MetroMania.Infrastructure.Orleans.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MetroMania.Infrastructure.Orleans;

public static class DependencyInjection
{
    public static IServiceCollection AddOrleansClient(this IServiceCollection services)
    {
        services.AddScoped<IGameRunnerService, GameRunnerService>();
        services.AddScoped<GameRunnerValidationService>();
        services.AddScoped<IGameRunnerValidationService>(sp => sp.GetRequiredService<GameRunnerValidationService>());
        services.AddScoped<IScriptValidationService>(sp => sp.GetRequiredService<GameRunnerValidationService>());

        return services;
    }
}
