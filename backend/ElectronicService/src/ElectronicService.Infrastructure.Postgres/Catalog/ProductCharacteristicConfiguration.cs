using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog;

public sealed class ProductCharacteristicConfiguration
    : IEntityTypeConfiguration<ProductCharacteristic>
{
    public void Configure(EntityTypeBuilder<ProductCharacteristic> builder)
    {
        builder.ToTable("product_characteristic_values", table =>
        {
            table.HasCheckConstraint(
                "ck_product_characteristic_values_value_data_type_not_none",
                "\"value_data_type\" <> 'None'");

            table.HasCheckConstraint(
                "ck_product_characteristic_values_only_one_value_type",
                """
                (
                    "value_data_type" = 'Text'
                    AND "value_text" IS NOT NULL
                    AND "value_number" IS NULL
                    AND "value_boolean" IS NULL
                )
                OR
                (
                    "value_data_type" = 'Number'
                    AND "value_text" IS NULL
                    AND "value_number" IS NOT NULL
                    AND "value_boolean" IS NULL
                )
                OR
                (
                    "value_data_type" = 'Boolean'
                    AND "value_text" IS NULL
                    AND "value_number" IS NULL
                    AND "value_boolean" IS NOT NULL
                )
                """);
        });

        builder.HasKey(characteristic => characteristic.Id);

        builder.Property(characteristic => characteristic.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(characteristic => characteristic.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(characteristic => characteristic.CharacteristicDefinitionId)
            .HasColumnName("characteristic_definition_id")
            .IsRequired();

        builder.OwnsOne(characteristic => characteristic.Value, valueBuilder =>
        {
            valueBuilder.Property(value => value.DataType)
                .HasColumnName("value_data_type")
                .HasMaxLength(32)
                .HasConversion<string>()
                .IsRequired();

            valueBuilder.Property(value => value.TextValue)
                .HasColumnName("value_text")
                .HasMaxLength(1000);

            valueBuilder.Property(value => value.NumberValue)
                .HasColumnName("value_number")
                .HasPrecision(18, 4);

            valueBuilder.Property(value => value.BooleanValue)
                .HasColumnName("value_boolean");
        });

        builder.Navigation(characteristic => characteristic.Value)
            .IsRequired();

        builder.HasOne<Product>()
            .WithMany(product => product.Characteristics)
            .HasForeignKey(characteristic => characteristic.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<CharacteristicDefinition>()
            .WithMany()
            .HasForeignKey(characteristic => characteristic.CharacteristicDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(characteristic => new
            {
                characteristic.ProductId,
                characteristic.CharacteristicDefinitionId
            })
            .IsUnique();

        builder.HasIndex(characteristic => characteristic.CharacteristicDefinitionId);
    }
}