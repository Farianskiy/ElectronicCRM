using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ProductTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog;

public sealed class ProductTypeCharacteristicConfiguration
    : IEntityTypeConfiguration<ProductTypeCharacteristic>
{
    public void Configure(EntityTypeBuilder<ProductTypeCharacteristic> builder)
    {
        builder.ToTable("product_type_characteristics", table =>
        {
            table.HasCheckConstraint(
                "ck_product_type_characteristics_replacement_match_mode_not_none_when_used",
                "\"is_used_for_replacement\" = false OR \"replacement_match_mode\" <> 'None'");

            table.HasCheckConstraint(
                "ck_product_type_characteristics_replacement_weight_not_negative",
                "\"replacement_weight\" >= 0");
        });

        builder.HasKey(productTypeCharacteristic => productTypeCharacteristic.Id);

        builder.Property(productTypeCharacteristic => productTypeCharacteristic.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(productTypeCharacteristic => productTypeCharacteristic.ProductTypeId)
            .HasColumnName("product_type_id")
            .IsRequired();

        builder.Property(productTypeCharacteristic => productTypeCharacteristic.CharacteristicDefinitionId)
            .HasColumnName("characteristic_definition_id")
            .IsRequired();

        builder.Property(productTypeCharacteristic => productTypeCharacteristic.IsRequired)
            .HasColumnName("is_required")
            .IsRequired();

        builder.Property(productTypeCharacteristic => productTypeCharacteristic.IsFilterable)
            .HasColumnName("is_filterable")
            .IsRequired();

        builder.Property(productTypeCharacteristic => productTypeCharacteristic.IsUsedForReplacement)
            .HasColumnName("is_used_for_replacement")
            .IsRequired();

        builder.Property(productTypeCharacteristic => productTypeCharacteristic.ReplacementMatchMode)
            .HasColumnName("replacement_match_mode")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(productTypeCharacteristic => productTypeCharacteristic.ReplacementWeight)
            .HasColumnName("replacement_weight")
            .IsRequired();

        builder.HasOne<ProductType>()
            .WithMany(productType => productType.Characteristics)
            .HasForeignKey(productTypeCharacteristic => productTypeCharacteristic.ProductTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<CharacteristicDefinition>()
            .WithMany()
            .HasForeignKey(productTypeCharacteristic => productTypeCharacteristic.CharacteristicDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(productTypeCharacteristic => new
            {
                productTypeCharacteristic.ProductTypeId,
                productTypeCharacteristic.CharacteristicDefinitionId
            })
            .IsUnique();

        builder.HasIndex(productTypeCharacteristic =>
            productTypeCharacteristic.CharacteristicDefinitionId);
    }
}