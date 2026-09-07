using ElectronicService.Domain.Catalog.PriceCalculations;
using ElectronicService.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

public sealed class CatalogPriceCalculationConfiguration
    : IEntityTypeConfiguration<CatalogPriceCalculation>
{
    public void Configure(
        EntityTypeBuilder<CatalogPriceCalculation> builder)
    {
        builder.ToTable(
            "catalog_price_calculations",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_catalog_price_calculations_status",
                    "\"status\" <> 'None'");

                table.HasCheckConstraint(
                    "ck_catalog_price_calculations_currency",
                    "char_length(\"currency\") = 3");

                table.HasCheckConstraint(
                    "ck_catalog_price_calculations_total",
                    "\"total_amount\" >= 0");
            });

        builder.HasKey(calculation =>
            calculation.Id);

        builder.Property(calculation =>
                calculation.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(calculation =>
                calculation.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(calculation =>
                calculation.Title)
            .HasColumnName("title")
            .HasMaxLength(
                CatalogPriceCalculation.MaximumTitleLength)
            .IsRequired();

        builder.Property(calculation =>
                calculation.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsFixedLength()
            .IsRequired();

        builder.Property(calculation =>
                calculation.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(calculation =>
                calculation.TotalAmount)
            .HasColumnName("total_amount")
            .HasPrecision(22, 2)
            .IsRequired();

        builder.Property(calculation =>
                calculation.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(calculation =>
                calculation.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(calculation =>
                calculation.CompletedAtUtc)
            .HasColumnName("completed_at_utc");

        builder.Property(calculation =>
                calculation.ArchivedAtUtc)
            .HasColumnName("archived_at_utc");

        builder.Property(calculation =>
                calculation.Version)
            .IsRowVersion();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(calculation =>
                calculation.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(calculation =>
                calculation.Lines)
            .WithOne()
            .HasForeignKey(line =>
                line.CalculationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(calculation =>
                calculation.Lines)
            .UsePropertyAccessMode(
                PropertyAccessMode.Field);

        builder.HasMany(calculation =>
                calculation.ManufacturerDiscounts)
            .WithOne()
            .HasForeignKey(discount =>
                discount.CalculationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(calculation =>
                calculation.ManufacturerDiscounts)
            .UsePropertyAccessMode(
                PropertyAccessMode.Field);

        builder.HasIndex(calculation =>
                new
                {
                    calculation.CreatedByUserId,
                    calculation.Status,
                    calculation.CreatedAtUtc
                })
            .IsDescending(
                false,
                false,
                true)
            .HasDatabaseName(
                "ix_catalog_price_calculations_user_status_created");

        builder.HasIndex(calculation =>
                new
                {
                    calculation.CreatedByUserId,
                    calculation.CreatedAtUtc
                })
            .IsDescending(
                false,
                true)
            .HasDatabaseName(
                "ix_catalog_price_calculations_user_created");
    }
}