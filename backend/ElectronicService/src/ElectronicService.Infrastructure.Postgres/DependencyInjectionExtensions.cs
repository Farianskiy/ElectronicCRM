using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Core.Users;
using ElectronicService.Infrastructure.Postgres.Data;
using ElectronicService.Infrastructure.Postgres.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ElectronicService.Infrastructure.Postgres;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddInfrastructurePostgres(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContextPool<ElectronicDbContext>((serviceProvider, options) =>
        {
            string connectionString = configuration.GetConnectionString("Database")
                ?? throw new InvalidOperationException(
                    "Connection string 'Database' is missing.");

            IHostEnvironment hostEnvironment = serviceProvider.GetRequiredService<IHostEnvironment>();
            ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            options.UseNpgsql(connectionString);

            if (hostEnvironment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
            options.UseLoggerFactory(loggerFactory);
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}