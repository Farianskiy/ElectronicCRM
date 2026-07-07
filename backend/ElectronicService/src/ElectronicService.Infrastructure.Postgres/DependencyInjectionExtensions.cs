using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Core.Catalog.Import.ImportProductsFromExcel;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Infrastructure.Postgres.Catalog.Import;
using ElectronicService.Infrastructure.Postgres.Catalog.Queries;
using ElectronicService.Infrastructure.Postgres.Catalog.Repositories;
using ElectronicService.Infrastructure.Postgres.Catalog.Seeding;
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
        services.AddScoped<CatalogDataSeeder>();
        services.AddScoped<ImportProductsFromExcelCommandHandler>();
        services.AddScoped<IProductsExcelImporter, ProductExcelImportService>();
        services.AddScoped<ICatalogProductsReader, CatalogProductsReader>();
        services.AddScoped<ICatalogProductReplacementsReader, CatalogProductReplacementsReader>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICatalogProductMetadataRepository, CatalogProductMetadataRepository>();

        return services;
    }
}