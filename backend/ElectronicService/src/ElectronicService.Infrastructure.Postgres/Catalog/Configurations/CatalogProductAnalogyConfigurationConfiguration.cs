using ElectronicService.Domain.Catalog.Manufacturers;
using ElectronicService.Domain.Catalog.Products;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

internal sealed class CatalogProductAnalogyConfigurationConfiguration
    : IEntityTypeConfiguration<CatalogProductAnalogyConfiguration>
{
    public void Configure(
        EntityTypeBuilder<CatalogProductAnalogyConfiguration> builder)
    {
        builder.ToTable("catalog_product_analogy_configurations", table =>
        {
            table.HasCheckConstraint(
                "ck_catalog_product_analogy_configuration_version",
                "\"version_number\" > 0");
        });

        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.ManufacturerId).HasColumnName("manufacturer_id").IsRequired();
        builder.Property(item => item.ProductTypeId).HasColumnName("product_type_id").IsRequired();
        builder.Property(item => item.VersionNumber).HasColumnName("version_number").IsRequired();
        builder.Property(item => item.Name).HasColumnName("name").HasMaxLength(CatalogProductAnalogyConfiguration.NameMaxLength).IsRequired();
        builder.Property(item => item.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.SettingsJson).HasColumnName("settings_json").HasMaxLength(CatalogProductAnalogyConfiguration.SettingsJsonMaxLength).IsRequired();
        builder.Property(item => item.SettingsHash).HasColumnName("settings_hash").HasMaxLength(CatalogProductAnalogyConfiguration.SettingsHashMaxLength).IsRequired();
        builder.Property(item => item.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(item => item.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.Property(item => item.EvaluatedAtUtc).HasColumnName("evaluated_at_utc");
        builder.Property(item => item.ActivatedAtUtc).HasColumnName("activated_at_utc");
        builder.Property(item => item.SupersededAtUtc).HasColumnName("superseded_at_utc");
        builder.Property(item => item.Version).IsRowVersion();

        builder.HasOne<Manufacturer>().WithMany().HasForeignKey(item => item.ManufacturerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductType>().WithMany().HasForeignKey(item => item.ProductTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => new { item.ManufacturerId, item.ProductTypeId, item.VersionNumber })
            .IsUnique()
            .HasDatabaseName("ux_catalog_product_analogy_configuration_scope_version");
        builder.HasIndex(item => new { item.ManufacturerId, item.ProductTypeId })
            .IsUnique()
            .HasFilter("\"status\" = 'Active'")
            .HasDatabaseName("ux_catalog_product_analogy_configuration_active_scope");
    }
}
