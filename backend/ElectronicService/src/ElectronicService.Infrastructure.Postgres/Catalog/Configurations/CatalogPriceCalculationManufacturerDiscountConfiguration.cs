using ElectronicService.Domain.Catalog.Manufacturers;
using ElectronicService.Domain.Catalog.PriceCalculations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

public sealed class CatalogPriceCalculationManufacturerDiscountConfiguration
    : IEntityTypeConfiguration<CatalogPriceCalculationManufacturerDiscount>
{
    public void Configure(
        EntityTypeBuilder<
            CatalogPriceCalculationManufacturerDiscount> builder)
    {
        builder.ToTable(
            "catalog_price_calculation_manufacturer_discounts",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_catalog_price_calculation_discounts_percent",
                    "\"discount_percent\" >= 0 "
                    + "AND \"discount_percent\" <= 100");
            });

        builder.HasKey(discount =>
            discount.Id);

        builder.Property(discount =>
                discount.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(discount =>
                discount.CalculationId)
            .HasColumnName("calculation_id")
            .IsRequired();

        builder.Property(discount =>
                discount.ManufacturerId)
            .HasColumnName("manufacturer_id")
            .IsRequired();

        builder.Property(discount =>
                discount.DiscountPercent)
            .HasColumnName("discount_percent")
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(discount =>
                discount.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(discount =>
                discount.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasOne<Manufacturer>()
            .WithMany()
            .HasForeignKey(discount =>
                discount.ManufacturerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(discount =>
                new
                {
                    discount.CalculationId,
                    discount.ManufacturerId
                })
            .IsUnique()
            .HasDatabaseName(
                "ux_catalog_price_calculation_discounts_calculation_manufacturer");

        builder.HasIndex(discount =>
                discount.ManufacturerId)
            .HasDatabaseName(
                "ix_catalog_price_calculation_discounts_manufacturer");
    }
}