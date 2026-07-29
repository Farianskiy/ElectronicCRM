using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Core.Catalog.Assistant.Abstractions;
using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.Abstractions;
using ElectronicService.Core.Catalog.Dictionaries.Abstractions;
using ElectronicService.Core.Catalog.Import.ImportProductsFromExcel;
using ElectronicService.Core.Catalog.Metadata.Abstractions;
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
using ElectronicService.Core.Catalog.ProductTypes.Abstractions;
using ElectronicService.Core.Catalog.CharacteristicDefinitions.Abstractions;
using ElectronicService.Core.Catalog.Products.Audit;
using ElectronicService.Core.Catalog.Products.GetAuditHistory;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Infrastructure.Postgres.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.ApplyCatalogImportBatch;
using ElectronicService.Infrastructure.Postgres.Catalog.ImportBatches.Cleanup;
using ElectronicService.Core.Catalog.ImportBatches.GetCatalogImportAppliedProducts;

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

        services.AddOptions<CatalogImportCleanupOptions>()
            .Bind(configuration.GetSection(
                CatalogImportCleanupOptions.SectionName))
            .Validate(
                options =>
                    options.RetentionDays is >= 1 and <= 3650,
                "CatalogImportCleanup:RetentionDays must be between 1 and 3650.")
            .Validate(
                options =>
                    options.InitialDelayMinutes is >= 0 and <= 1440,
                "CatalogImportCleanup:InitialDelayMinutes must be between 0 and 1440.")
            .Validate(
                options =>
                    options.IntervalHours is >= 1 and <= 720,
                "CatalogImportCleanup:IntervalHours must be between 1 and 720.")
            .Validate(
                options =>
                    options.BatchSize is >= 1 and <= 5000,
                "CatalogImportCleanup:BatchSize must be between 1 and 5000.")
            .ValidateOnStart();

        services.AddScoped<CatalogImportCleanupService>();
        services.AddHostedService<CatalogImportCleanupHostedService>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<CatalogDataSeeder>();
        services.AddScoped<ImportProductsFromExcelCommandHandler>();
        services.AddScoped<IProductsExcelImporter, ProductExcelImportService>();
        services.AddScoped<ICatalogProductsReader, CatalogProductsReader>();
        services.AddScoped<ICatalogProductReplacementsReader, CatalogProductReplacementsReader>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICatalogProductMetadataRepository, CatalogProductMetadataRepository>();
        services.AddScoped<ICatalogMetadataReader, CatalogMetadataReader>();
        services.AddScoped<ICatalogDictionaryReader, CatalogDictionaryReader>();
        services.AddScoped<ICatalogDictionaryRepository, CatalogDictionaryRepository>();
        services.AddScoped<ICatalogAssistantUnknownTermResolver, CatalogAssistantUnknownTermResolver>();
        services.AddScoped<ICatalogAssistantDictionarySuggestionRepository, CatalogAssistantDictionarySuggestionRepository>();
        services.AddScoped<ICatalogAssistantDictionarySuggestionReader, CatalogAssistantDictionarySuggestionReader>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ICatalogProductTypeSchemaReader, CatalogProductTypeSchemaReader>();
        services.AddScoped<IProductTypeSchemaRepository, ProductTypeSchemaRepository>();
        services.AddScoped<ICatalogCharacteristicDefinitionsReader, CatalogCharacteristicDefinitionsReader>();
        services.AddScoped<ICharacteristicDefinitionRepository, CharacteristicDefinitionRepository>();
        services.AddScoped<IProductAuditRepository, ProductAuditRepository>();
        services.AddScoped<IProductAuditHistoryReader, ProductAuditHistoryReader>();
        services.AddScoped<ICatalogImportBatchRepository, CatalogImportBatchRepository>();
        services.AddScoped<ICatalogImportWorkbookAnalyzer, CatalogImportWorkbookAnalyzer>();
        services.AddScoped<ICatalogImportBatchApplier, CatalogImportBatchApplier>();
        services.AddScoped<ICatalogImportAppliedProductsReader, CatalogImportAppliedProductsReader>();

        return services;
    }
}