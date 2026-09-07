using ElectronicService.Domain.Catalog.Manufacturers;
using ElectronicService.Domain.Catalog.PriceCalculations;
using ElectronicService.Domain.Catalog.PriceLists;
using ElectronicService.Domain.Catalog.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

public sealed class CatalogPriceCalculationLineConfiguration
    : IEntityTypeConfiguration<CatalogPriceCalculationLine>
{
    public void Configure(
        EntityTypeBuilder<CatalogPriceCalculationLine> builder)
    {
        builder.ToTable(
            "catalog_price_calculation_lines",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_catalog_price_calculation_lines_quantity",
                    "\"quantity\" > 0 "
                    + $"AND \"quantity\" <= {CatalogPriceCalculationLine.MaximumQuantity}");

                table.HasCheckConstraint(
                    "ck_catalog_price_calculation_lines_base_price",
                    "\"base_price_amount\" >= 0 "
                    + $"AND \"base_price_amount\" <= {CatalogPriceCalculationLine.MaximumPriceAmount}");

                table.HasCheckConstraint(
                    "ck_catalog_price_calculation_lines_mrc_price",
                    "\"mrc_price_amount\" IS NULL "
                    + "OR (\"mrc_price_amount\" >= 0 "
                    + $"AND \"mrc_price_amount\" <= {CatalogPriceCalculationLine.MaximumPriceAmount})");

                table.HasCheckConstraint(
                    "ck_catalog_price_calculation_lines_discount",
                    "\"discount_percent\" >= 0 "
                    + "AND \"discount_percent\" <= 100");

                table.HasCheckConstraint(
                    "ck_catalog_price_calculation_lines_project_price",
                    "\"project_price_amount\" >= 0 "
                    + "AND \"project_price_amount\" <= \"base_price_amount\"");

                table.HasCheckConstraint(
                    "ck_catalog_price_calculation_lines_total",
                    "\"total_amount\" >= 0");
            });

        builder.HasKey(line =>
            line.Id);

        builder.Property(line =>
                line.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(line =>
                line.CalculationId)
            .HasColumnName("calculation_id")
            .IsRequired();

        builder.Property(line =>
                line.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(line =>
                line.ManufacturerId)
            .HasColumnName("manufacturer_id")
            .IsRequired();

        builder.Property(line =>
                line.PriceListId)
            .HasColumnName("price_list_id")
            .IsRequired();

        builder.Property(line =>
                line.PriceListRowId)
            .HasColumnName("price_list_row_id")
            .IsRequired();

        builder.Property(line =>
                line.Article)
            .HasColumnName("article")
            .HasMaxLength(
                CatalogPriceCalculationLine.MaximumArticleLength)
            .IsRequired();

        builder.Property(line =>
                line.Name)
            .HasColumnName("name")
            .HasMaxLength(
                CatalogPriceCalculationLine.MaximumNameLength)
            .IsRequired();

        builder.Property(line =>
                line.ManufacturerName)
            .HasColumnName("manufacturer_name")
            .HasMaxLength(
                CatalogPriceCalculationLine
                    .MaximumManufacturerNameLength)
            .IsRequired();

        builder.Property(line =>
                line.Unit)
            .HasColumnName("unit")
            .HasMaxLength(
                CatalogPriceCalculationLine.MaximumUnitLength);

        builder.Property(line =>
                line.Quantity)
            .HasColumnName("quantity")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(line =>
                line.BasePriceAmount)
            .HasColumnName("base_price_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(line =>
                line.MrcPriceAmount)
            .HasColumnName("mrc_price_amount")
            .HasPrecision(18, 2);

        builder.Property(line =>
                line.DiscountPercent)
            .HasColumnName("discount_percent")
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(line =>
                line.ProjectPriceAmount)
            .HasColumnName("project_price_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(line =>
                line.TotalAmount)
            .HasColumnName("total_amount")
            .HasPrecision(22, 2)
            .IsRequired();

        builder.Property(line =>
                line.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(line =>
                line.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(line =>
                line.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Manufacturer>()
            .WithMany()
            .HasForeignKey(line =>
                line.ManufacturerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CatalogPriceList>()
            .WithMany()
            .HasForeignKey(line =>
                line.PriceListId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CatalogPriceListRow>()
            .WithMany()
            .HasForeignKey(line =>
                line.PriceListRowId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(line =>
                new
                {
                    line.CalculationId,
                    line.PriceListRowId
                })
            .IsUnique()
            .HasDatabaseName(
                "ux_catalog_price_calculation_lines_calculation_price_row");

        builder.HasIndex(line =>
                new
                {
                    line.CalculationId,
                    line.ManufacturerId
                })
            .HasDatabaseName(
                "ix_catalog_price_calculation_lines_calculation_manufacturer");

        builder.HasIndex(line =>
                new
                {
                    line.CalculationId,
                    line.ProductId
                })
            .HasDatabaseName(
                "ix_catalog_price_calculation_lines_calculation_product");

        builder.HasIndex(line =>
                line.PriceListId)
            .HasDatabaseName(
                "ix_catalog_price_calculation_lines_price_list");

        builder.HasIndex(line =>
                line.PriceListRowId)
            .HasDatabaseName(
                "ix_catalog_price_calculation_lines_price_list_row");
    }
}