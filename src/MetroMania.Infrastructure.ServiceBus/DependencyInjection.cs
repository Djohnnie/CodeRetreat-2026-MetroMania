using MetroMania.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace MetroMania.Infrastructure.ServiceBus;

public static class DependencyInjection
{
    public static IServiceCollection AddServiceBus(this IServiceCollection services)
    {
        services.AddSingleton<ISubmissionQueueService, SubmissionQueueService>();
        services.AddSingleton<ICleanupQueueService, CleanupQueueService>();
        return services;
    }
}
