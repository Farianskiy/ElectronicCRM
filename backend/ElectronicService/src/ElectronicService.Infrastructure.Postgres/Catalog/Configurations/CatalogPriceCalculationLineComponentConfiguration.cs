using ElectronicService.Domain.Catalog.Components;
using ElectronicService.Domain.Catalog.Manufacturers;
using ElectronicService.Domain.Catalog.PriceCalculations;
using ElectronicService.Domain.Catalog.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

public sealed class CatalogPriceCalculationLineComponentConfiguration
    : IEntityTypeConfiguration<
        CatalogPriceCalculationLineComponent>
{
    public void Configure(
        EntityTypeBuilder<
            CatalogPriceCalculationLineComponent> builder)
    {
        builder.ToTable(
            "catalog_price_calculation_line_components",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_catalog_price_calculation_line_components_quantity",
                    "\"quantity_per_unit\" >= 1 "
                    + $"AND \"quantity_per_unit\" <= {CatalogPriceCalculationLineComponent.MaximumQuantityPerUnit}");

                table.HasCheckConstraint(
                    "ck_catalog_price_calculation_line_components_base_price",
                    "\"base_price_amount\" >= 0 "
                    + $"AND \"base_price_amount\" <= {CatalogPriceCalculationLine.MaximumPriceAmount}");

                table.HasCheckConstraint(
                    "ck_catalog_price_calculation_line_components_discount",
                    "\"discount_percent\" >= 0 "
                    + "AND \"discount_percent\" <= 100");

                table.HasCheckConstraint(
                    "ck_catalog_price_calculation_line_components_project_price",
                    "\"project_price_amount\" >= 0 "
                    + "AND \"project_price_amount\" <= \"base_price_amount\"");

                table.HasCheckConstraint(
                    "ck_catalog_price_calculation_line_components_total_quantity",
                    "\"total_quantity\" > 0");

                table.HasCheckConstraint(
                    "ck_catalog_price_calculation_line_components_total",
                    "\"total_amount\" >= 0");
            });

        builder.HasKey(component =>
            component.Id);

        builder.Property(component =>
                component.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(component =>
                component.CalculationLineId)
            .HasColumnName("calculation_line_id")
            .IsRequired();

        builder.Property(component =>
                component.NeedDefinitionId)
            .HasColumnName("need_definition_id")
            .IsRequired();

        builder.Property(component =>
                component.NeedName)
            .HasColumnName("need_name")
            .HasMaxLength(
                CatalogPriceCalculationLineComponent
                    .MaximumNeedNameLength)
            .IsRequired();

        builder.Property(component =>
                component.ComponentProductId)
            .HasColumnName("component_product_id")
            .IsRequired();

        builder.Property(component =>
                component.ManufacturerId)
            .HasColumnName("manufacturer_id")
            .IsRequired();

        builder.Property(component =>
                component.Article)
            .HasColumnName("article")
            .HasMaxLength(
                CatalogPriceCalculationLine
                    .MaximumArticleLength)
            .IsRequired();

        builder.Property(component =>
                component.Name)
            .HasColumnName("name")
            .HasMaxLength(
                CatalogPriceCalculationLine
                    .MaximumNameLength)
            .IsRequired();

        builder.Property(component =>
                component.ManufacturerName)
            .HasColumnName("manufacturer_name")
            .HasMaxLength(
                CatalogPriceCalculationLine
                    .MaximumManufacturerNameLength)
            .IsRequired();

        builder.Property(component =>
                component.QuantityPerUnit)
            .HasColumnName("quantity_per_unit")
            .IsRequired();

        builder.Property(component =>
                component.BasePriceAmount)
            .HasColumnName("base_price_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(component =>
                component.DiscountPercent)
            .HasColumnName("discount_percent")
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(component =>
                component.ProjectPriceAmount)
            .HasColumnName("project_price_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(component =>
                component.TotalQuantity)
            .HasColumnName("total_quantity")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(component =>
                component.TotalAmount)
            .HasColumnName("total_amount")
            .HasPrecision(22, 2)
            .IsRequired();

        builder.Property(component =>
                component.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(component =>
                component.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasOne<CatalogPriceCalculationLine>()
            .WithMany(line =>
                line.Components)
            .HasForeignKey(component =>
                component.CalculationLineId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ComponentNeedDefinition>()
            .WithMany()
            .HasForeignKey(component =>
                component.NeedDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(component =>
                component.ComponentProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Manufacturer>()
            .WithMany()
            .HasForeignKey(component =>
                component.ManufacturerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(component =>
                new
                {
                    component.CalculationLineId,
                    component.NeedDefinitionId,
                    component.ComponentProductId
                })
            .IsUnique()
            .HasDatabaseName(
                "ux_catalog_price_calculation_line_components_line_need_product");

        builder.HasIndex(component =>
                component.CalculationLineId)
            .HasDatabaseName(
                "ix_catalog_price_calculation_line_components_line");

        builder.HasIndex(component =>
                component.NeedDefinitionId)
            .HasDatabaseName(
                "ix_catalog_price_calculation_line_components_need");

        builder.HasIndex(component =>
                component.ComponentProductId)
            .HasDatabaseName(
                "ix_catalog_price_calculation_line_components_product");

        builder.HasIndex(component =>
                component.ManufacturerId)
            .HasDatabaseName(
                "ix_catalog_price_calculation_line_components_manufacturer");
    }
}