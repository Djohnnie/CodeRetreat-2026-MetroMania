using MetroMania.Application.Interfaces;
using MetroMania.Domain.Interfaces;
using MetroMania.Infrastructure.Persistence;
using MetroMania.Infrastructure.Repositories;
using MetroMania.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MetroMania.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ILevelRepository, LevelRepository>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        return services;
    }
}
