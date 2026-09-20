using ElectronicService.Domain.Catalog.Recognition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

internal sealed class CatalogRecognitionRuleSetVersionConfiguration
    : IEntityTypeConfiguration<CatalogRecognitionRuleSetVersion>
{
    public void Configure(EntityTypeBuilder<CatalogRecognitionRuleSetVersion> builder)
    {
        builder.ToTable("catalog_recognition_rule_set_versions", table =>
        {
            table.HasCheckConstraint("ck_rule_set_version_number", "\"version_number\" > 0");
            table.HasCheckConstraint("ck_rule_set_version_name", "char_length(btrim(\"name\")) > 0");
        });

        builder.HasKey(version => version.Id);

        builder.Property(version => version.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(version => version.ManufacturerId).HasColumnName("manufacturer_id").IsRequired();
        builder.Property(version => version.ProductTypeId).HasColumnName("product_type_id").IsRequired();
        builder.Property(version => version.VersionNumber).HasColumnName("version_number").IsRequired();
        builder.Property(version => version.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(version => version.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(version => version.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasMany(version => version.Entries)
            .WithOne()
            .HasForeignKey(entry => entry.VersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(version => version.Entries)
            .HasField("_entries")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(version => new
        {
            version.ManufacturerId,
            version.ProductTypeId,
            version.VersionNumber
        })
            .IsUnique()
            .HasDatabaseName("ux_rule_set_version_scope_number");
    }
}