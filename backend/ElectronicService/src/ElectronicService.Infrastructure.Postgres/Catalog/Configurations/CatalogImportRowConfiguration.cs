using ElectronicService.Domain.Catalog.ImportBatches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres
    .Catalog.Configurations;

public sealed class CatalogImportRowConfiguration
    : IEntityTypeConfiguration<CatalogImportRow>
{
    public void Configure(
        EntityTypeBuilder<CatalogImportRow> builder)
    {
        builder.ToTable(
            "catalog_import_rows",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_catalog_import_rows_number",
                    "\"row_number\" >= 2");

                table.HasCheckConstraint(
                    "ck_catalog_import_rows_status_not_none",
                    "\"status\" <> 'None'");
            });

        builder.HasKey(row =>
            row.Id);

        builder.Property(row =>
                row.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(row =>
                row.BatchId)
            .HasColumnName("batch_id")
            .IsRequired();

        builder.Property(row =>
                row.RowNumber)
            .HasColumnName("row_number")
            .IsRequired();

        builder.Property(row =>
                row.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(row =>
                row.RawDataJson)
            .HasColumnName("raw_data_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(row =>
                row.NormalizedDataJson)
            .HasColumnName(
                "normalized_data_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(row =>
                row.IssuesJson)
            .HasColumnName("issues_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(row =>
                row.WarningsJson)
            .HasColumnName("warnings_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasOne<CatalogImportBatch>()
            .WithMany()
            .HasForeignKey(row =>
                row.BatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(row =>
                new
                {
                    row.BatchId,
                    row.RowNumber
                })
            .IsUnique()
            .HasDatabaseName(
                "ux_catalog_import_rows_batch_number");

        /*
         * Основной индекс для frontend:
         *
         * получить ошибки конкретного batch
         * с сортировкой по Excel-строке.
         */
        builder.HasIndex(row =>
                new
                {
                    row.BatchId,
                    row.Status,
                    row.RowNumber
                })
            .HasDatabaseName(
                "ix_catalog_import_rows_batch_status_number");
    }
}