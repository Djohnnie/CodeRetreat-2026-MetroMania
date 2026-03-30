using MetroMania.Application.Interfaces;
using MetroMania.Domain.Interfaces;
using MetroMania.Infrastructure.Sql.Persistence;
using MetroMania.Infrastructure.Sql.Repositories;
using MetroMania.Infrastructure.Sql.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MetroMania.Infrastructure.Sql;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ILevelRepository, LevelRepository>();
        services.AddScoped<ISubmissionRepository, SubmissionRepository>();
        services.AddScoped<ISubmissionScoreRepository, SubmissionScoreRepository>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        return services;
    }
}
