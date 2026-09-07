using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Core.Catalog.Assistant.Abstractions;
using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.Abstractions;
using ElectronicService.Core.Catalog.CharacteristicDefinitions.Abstractions;
using ElectronicService.Core.Catalog.Dictionaries.Abstractions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Core.Catalog.ImportBatches.ApplyCatalogImportBatch;
using ElectronicService.Core.Catalog.ImportBatches.ExportCatalogImportErrorReport;
using ElectronicService.Core.Catalog.ImportBatches.GetCatalogImportAppliedProducts;
using ElectronicService.Core.Catalog.Metadata.Abstractions;
using ElectronicService.Core.Catalog.PriceCalculations.Abstractions;
using ElectronicService.Core.Catalog.PriceLists.Abstractions;
using ElectronicService.Core.Catalog.PriceLists.Import;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.Products.Audit;
using ElectronicService.Core.Catalog.Products.GetAuditHistory;
using ElectronicService.Core.Catalog.Products.StockImport;
using ElectronicService.Core.Catalog.ProductTypes.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Infrastructure.Postgres.Catalog.ImportBatches;
using ElectronicService.Infrastructure.Postgres.Catalog.ImportBatches.Cleanup;
using ElectronicService.Infrastructure.Postgres.Catalog.ImportBatches.Reports;
using ElectronicService.Infrastructure.Postgres.Catalog.PriceCalculations;
using ElectronicService.Infrastructure.Postgres.Catalog.PriceLists;
using ElectronicService.Infrastructure.Postgres.Catalog.Queries;
using ElectronicService.Infrastructure.Postgres.Catalog.Repositories;
using ElectronicService.Infrastructure.Postgres.Catalog.Products;
using ElectronicService.Infrastructure.Postgres.Catalog.Seeding;
using ElectronicService.Infrastructure.Postgres.Data;
using ElectronicService.Infrastructure.Postgres.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ElectronicService.Core.Catalog.Manufacturers.Resolution;
using ElectronicService.Infrastructure.Postgres.Catalog.Manufacturers;
using ElectronicService.Core.Catalog.Manufacturers.Abstractions;
using ElectronicService.Core.Catalog.Manufacturers.CreateFromUnresolvedPhrase;

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
        services.AddScoped<ICatalogProductsReader, CatalogProductsReader>();
        services.AddScoped<ICatalogProductReplacementsReader, CatalogProductReplacementsReader>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICatalogStockWorkbookImporter, CatalogStockWorkbookImporter>();
        services.AddScoped<ICatalogProductMetadataRepository, CatalogProductMetadataRepository>();
        services.AddScoped<IManufacturerResolver, PostgresManufacturerResolver>();
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
        services.AddScoped<ICatalogImportErrorReportGenerator, CatalogImportErrorReportGenerator>();
        services.AddScoped<ICatalogPriceCalculationRepository, CatalogPriceCalculationRepository>();
        services.AddScoped<ICatalogPriceCalculationReader, CatalogPriceCalculationReader>();
        services.AddScoped<ICatalogPriceCalculationProductSearchReader, CatalogPriceCalculationProductSearchReader>();
        services.AddScoped<ICatalogActiveProductPriceReader, CatalogActiveProductPriceReader>();
        services.AddScoped<ICatalogPriceListRepository, CatalogPriceListRepository>();
        services.AddScoped<ICatalogPriceListReader, CatalogPriceListReader>();
        services.AddSingleton<ICatalogPriceListWorkbookReader, CatalogPriceListWorkbookReader>();
        services.AddScoped<ICatalogPriceListProcessor, CatalogPriceListProcessor>();
        services.AddScoped<ICatalogCharacteristicRecognitionProfileReader, CatalogCharacteristicRecognitionProfileReader>();
        services.AddScoped<ICatalogCharacteristicRecognitionProfileRepository, CatalogCharacteristicRecognitionProfileRepository>();
        services.AddScoped<ICatalogRecognitionFeedbackRepository, CatalogRecognitionFeedbackRepository>();
        services.AddScoped<ICatalogRecognitionDatasetReader, CatalogRecognitionDatasetReader>();
        services.AddScoped<ICatalogRecognitionCandidateRepository, CatalogRecognitionCandidateRepository>();
        services.AddScoped<IManufacturerRepository, ManufacturerRepository>();
        services.AddScoped<IManufacturerAliasRepository, ManufacturerAliasRepository>();
        services.AddScoped<IManufacturerNoisePhraseRepository, ManufacturerNoisePhraseRepository>();


        return services;
    }
}
