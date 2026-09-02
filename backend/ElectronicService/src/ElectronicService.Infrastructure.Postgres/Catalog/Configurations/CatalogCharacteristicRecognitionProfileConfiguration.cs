using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Domain.Catalog.Recognition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

internal sealed class CatalogCharacteristicRecognitionProfileConfiguration
    : IEntityTypeConfiguration<CatalogCharacteristicRecognitionProfile>
{
    public void Configure(
        EntityTypeBuilder<CatalogCharacteristicRecognitionProfile> builder)
    {
        builder.ToTable(
            "catalog_characteristic_recognition_profiles",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_recognition_profiles_strategy_kind",
                    "\"strategy_kind\" IN (" +
                    "'NumericWithUnit', " +
                    "'PoleCount', " +
                    "'EnumToken', " +
                    "'BooleanAlias', " +
                    "'Dimensions', " +
                    "'Dictionary'" +
                    ")");

                table.HasCheckConstraint(
                    "ck_recognition_profiles_priority",
                    $"\"priority\" >= " +
                    $"{CatalogCharacteristicRecognitionProfile.MinimumPriority} " +
                    $"AND \"priority\" <= " +
                    $"{CatalogCharacteristicRecognitionProfile.MaximumPriority}");

                table.HasCheckConstraint(
                    "ck_recognition_profiles_minimum_confidence",
                    "\"minimum_confidence\" > 0 " +
                    "AND \"minimum_confidence\" <= 1");

                table.HasCheckConstraint(
                    "ck_recognition_profiles_configuration_object",
                    "jsonb_typeof(\"configuration_json\") = 'object'");
            });

        builder.HasKey(profile => profile.Id);

        builder.Property(profile => profile.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(profile => profile.ProductTypeId)
            .HasColumnName("product_type_id")
            .IsRequired();

        builder.Property(profile => profile.CharacteristicDefinitionId)
            .HasColumnName("characteristic_definition_id")
            .IsRequired();

        builder.Property(profile => profile.StrategyKind)
            .HasColumnName("strategy_kind")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(profile => profile.Priority)
            .HasColumnName("priority")
            .IsRequired();

        builder.Property(profile => profile.MinimumConfidence)
            .HasColumnName("minimum_confidence")
            .HasPrecision(5, 4)
            .IsRequired();

        builder.Property(profile => profile.ConfigurationJson)
            .HasColumnName("configuration_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(profile => profile.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(profile => profile.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(profile => profile.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasOne<ProductType>()
            .WithMany()
            .HasForeignKey(profile => profile.ProductTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<CharacteristicDefinition>()
            .WithMany()
            .HasForeignKey(profile => profile.CharacteristicDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(profile => new
        {
            profile.ProductTypeId,
            profile.CharacteristicDefinitionId,
            profile.StrategyKind
        })
            .IsUnique()
            .HasDatabaseName(
                "ux_recognition_profiles_scope_strategy");

        builder.HasIndex(profile => new
        {
            profile.ProductTypeId,
            profile.IsActive
        })
            .HasDatabaseName(
                "ix_recognition_profiles_product_type_active");

        builder.HasIndex(profile => profile.CharacteristicDefinitionId)
            .HasDatabaseName(
                "ix_recognition_profiles_characteristic");
    }
}