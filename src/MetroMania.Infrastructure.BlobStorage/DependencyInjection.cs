using MetroMania.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace MetroMania.Infrastructure.BlobStorage;

public static class DependencyInjection
{
    public static IServiceCollection AddBlobStorage(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IRenderBlobStorage>(new RenderBlobStorage(connectionString));
        services.AddSingleton<IDebugBlobStorage>(new DebugBlobStorage(connectionString));
        return services;
    }
}
