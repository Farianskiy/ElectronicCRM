using ElectronicService.Domain.Catalog.Manufacturers;
using ElectronicService.Domain.Catalog.PriceLists;
using ElectronicService.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

public sealed class CatalogPriceListConfiguration
    : IEntityTypeConfiguration<CatalogPriceList>
{
    public void Configure(
        EntityTypeBuilder<CatalogPriceList> builder)
    {
        builder.ToTable(
            "catalog_price_lists",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_catalog_price_lists_status_not_none",
                    "\"status\" <> 'None'");

                table.HasCheckConstraint(
                    "ck_catalog_price_lists_file_size",
                    "\"file_size_bytes\" > 0 "
                    + $"AND \"file_size_bytes\" <= {CatalogPriceList.MaximumFileSizeBytes}");

                table.HasCheckConstraint(
                    "ck_catalog_price_lists_file_sha256",
                    "char_length(\"file_sha256\") = 64");

                table.HasCheckConstraint(
                    "ck_catalog_price_lists_currency",
                    "char_length(\"currency\") = 3");

                table.HasCheckConstraint(
                    "ck_catalog_price_lists_vat_rate",
                    "\"vat_rate_percent\" >= 0 "
                    + "AND \"vat_rate_percent\" <= 100");

                table.HasCheckConstraint(
                    "ck_catalog_price_lists_rows_count",
                    "\"rows_count\" >= 0");

                table.HasCheckConstraint(
                    "ck_catalog_price_lists_valid_rows_count",
                    "\"valid_rows_count\" >= 0");

                table.HasCheckConstraint(
                    "ck_catalog_price_lists_error_rows_count",
                    "\"error_rows_count\" >= 0");

                table.HasCheckConstraint(
                    "ck_catalog_price_lists_estimated_rows_count",
                    "\"estimated_rows_count\" >= 0");

                table.HasCheckConstraint(
                    "ck_catalog_price_lists_read_rows_count",
                    "\"read_rows_count\" >= 0");

                table.HasCheckConstraint(
                    "ck_catalog_price_lists_saved_rows_count",
                    "\"saved_rows_count\" >= 0");

                table.HasCheckConstraint(
                    "ck_catalog_price_lists_processing_progress",
                    "\"saved_rows_count\" <= \"read_rows_count\"");

                table.HasCheckConstraint(
                    "ck_catalog_price_lists_rows_statistics",
                    "\"valid_rows_count\" + \"error_rows_count\" <= \"rows_count\"");
            });

        builder.HasKey(priceList =>
            priceList.Id);

        builder.Property(priceList =>
                priceList.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(priceList =>
                priceList.ManufacturerId)
            .HasColumnName("manufacturer_id")
            .IsRequired();

        builder.Property(priceList =>
                priceList.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(priceList =>
                priceList.OriginalFileName)
            .HasColumnName("original_file_name")
            .HasMaxLength(
                CatalogPriceList.MaximumFileNameLength)
            .IsRequired();

        builder.Property(priceList =>
                priceList.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(
                CatalogPriceList.MaximumContentTypeLength)
            .IsRequired();

        builder.Property(priceList =>
                priceList.FileSizeBytes)
            .HasColumnName("file_size_bytes")
            .IsRequired();

        builder.Property(priceList =>
                priceList.FileSha256)
            .HasColumnName("file_sha256")
            .HasMaxLength(64)
            .IsFixedLength()
            .IsRequired();

        builder.Property(priceList =>
                priceList.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsFixedLength()
            .IsRequired();

        builder.Property(priceList =>
                priceList.VatRatePercent)
            .HasColumnName("vat_rate_percent")
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(priceList =>
                priceList.EffectiveDate)
            .HasColumnName("effective_date")
            .HasColumnType("date");

        builder.Property(priceList =>
                priceList.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(priceList =>
                priceList.RowsCount)
            .HasColumnName("rows_count")
            .IsRequired();

        builder.Property(priceList =>
                priceList.ValidRowsCount)
            .HasColumnName("valid_rows_count")
            .IsRequired();

        builder.Property(priceList =>
                priceList.ErrorRowsCount)
            .HasColumnName("error_rows_count")
            .IsRequired();

        builder.Property(priceList =>
                priceList.EstimatedRowsCount)
            .HasColumnName("estimated_rows_count")
            .IsRequired();

        builder.Property(priceList =>
                priceList.ReadRowsCount)
            .HasColumnName("read_rows_count")
            .IsRequired();

        builder.Property(priceList =>
                priceList.SavedRowsCount)
            .HasColumnName("saved_rows_count")
            .IsRequired();

        builder.Property(priceList =>
                priceList.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(priceList =>
                priceList.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(priceList =>
                priceList.ProcessedAtUtc)
            .HasColumnName("processed_at_utc");

        builder.Property(priceList =>
                priceList.ActivatedAtUtc)
            .HasColumnName("activated_at_utc");

        builder.Property(priceList =>
                priceList.ArchivedAtUtc)
            .HasColumnName("archived_at_utc");

        builder.Property(priceList =>
                priceList.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(
                CatalogPriceList.MaximumFailureReasonLength);

        builder.Property(priceList =>
                priceList.Version)
            .IsRowVersion();

        builder.HasOne<Manufacturer>()
            .WithMany()
            .HasForeignKey(priceList =>
                priceList.ManufacturerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(priceList =>
                priceList.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(priceList =>
                priceList.File)
            .WithOne()
            .HasForeignKey<CatalogPriceListFile>(
                file => file.PriceListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(priceList =>
                priceList.File)
            .IsRequired();

        builder.HasIndex(priceList =>
                new
                {
                    priceList.ManufacturerId,
                    priceList.Status
                })
            .HasDatabaseName(
                "ix_catalog_price_lists_manufacturer_status");

        builder.HasIndex(priceList =>
                new
                {
                    priceList.ManufacturerId,
                    priceList.EffectiveDate
                })
            .IsDescending(
                false,
                true)
            .HasDatabaseName(
                "ix_catalog_price_lists_manufacturer_effective_date");

        builder.HasIndex(priceList =>
                new
                {
                    priceList.ManufacturerId,
                    priceList.FileSha256
                })
            .IsUnique()
            .HasDatabaseName(
                "ux_catalog_price_lists_manufacturer_file_sha256");

        builder.HasIndex(priceList =>
                priceList.ManufacturerId)
            .IsUnique()
            .HasFilter("\"status\" = 'Active'")
            .HasDatabaseName(
                "ux_catalog_price_lists_active_manufacturer");
    }
}