using ElectronicService.Domain.Users;
using Microsoft.EntityFrameworkCore;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.Manufacturers;
using ElectronicService.Domain.Catalog.Products;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Domain.Catalog.Dictionaries;
using ElectronicService.Domain.Catalog.Audit;
using ElectronicService.Domain.Catalog.ImportBatches;

namespace ElectronicService.Infrastructure.Postgres.Data;

public sealed class ElectronicDbContext : DbContext
{
    public ElectronicDbContext(DbContextOptions<ElectronicDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductCharacteristic> ProductCharacteristics => Set<ProductCharacteristic>();

    public DbSet<ProductAlias> ProductAliases => Set<ProductAlias>();

    public DbSet<ProductType> ProductTypes => Set<ProductType>();

    public DbSet<ProductTypeCharacteristic> ProductTypeCharacteristics => Set<ProductTypeCharacteristic>();

    public DbSet<CharacteristicDefinition> CharacteristicDefinitions => Set<CharacteristicDefinition>();

    public DbSet<Manufacturer> Manufacturers => Set<Manufacturer>();

    public DbSet<CatalogDictionaryTerm> CatalogDictionaryTerms => Set<CatalogDictionaryTerm>();

    public DbSet<CatalogAssistantDictionarySuggestion> CatalogAssistantDictionarySuggestions => Set<CatalogAssistantDictionarySuggestion>();

    public DbSet<ProductAuditEntry> ProductAuditEntries => Set<ProductAuditEntry>();

    public DbSet<CatalogImportBatch> CatalogImportBatches => Set<CatalogImportBatch>();

    public DbSet<CatalogImportFile> CatalogImportFiles => Set<CatalogImportFile>();

    public DbSet<CatalogImportColumn> CatalogImportColumns => Set<CatalogImportColumn>();

    public DbSet<CatalogImportRow> CatalogImportRows => Set<CatalogImportRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ElectronicDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}