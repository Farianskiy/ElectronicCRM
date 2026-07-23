using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ImportBatches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres
    .Catalog.Configurations;

public sealed class CatalogImportColumnConfiguration
    : IEntityTypeConfiguration<CatalogImportColumn>
{
    public void Configure(
        EntityTypeBuilder<CatalogImportColumn> builder)
    {
        builder.ToTable(
            "catalog_import_columns",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_catalog_import_columns_number",
                    "\"source_column_number\" > 0");

                table.HasCheckConstraint(
                    "ck_catalog_import_columns_target_not_none",
                    "\"target_kind\" <> 'None'");

                table.HasCheckConstraint(
                    "ck_catalog_import_columns_confidence",
                    "\"confidence\" >= 0 " +
                    "AND \"confidence\" <= 1");

                table.HasCheckConstraint(
                    "ck_catalog_import_columns_characteristic_mapping",
                    "(" +
                    "\"target_kind\" = 'Characteristic' " +
                    "AND \"characteristic_definition_id\" " +
                    "IS NOT NULL" +
                    ") OR (" +
                    "\"target_kind\" <> 'Characteristic' " +
                    "AND \"characteristic_definition_id\" " +
                    "IS NULL" +
                    ")");

                table.HasCheckConstraint(
                    "ck_catalog_import_columns_unmapped_not_confirmed",
                    "\"target_kind\" <> 'Unmapped' " +
                    "OR \"is_confirmed\" = FALSE");
            });

        builder.HasKey(column =>
            column.Id);

        builder.Property(column =>
                column.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(column =>
                column.BatchId)
            .HasColumnName("batch_id")
            .IsRequired();

        builder.Property(column =>
                column.SourceColumnNumber)
            .HasColumnName(
                "source_column_number")
            .IsRequired();

        builder.Property(column =>
                column.SourceHeader)
            .HasColumnName("source_header")
            .HasMaxLength(
                CatalogImportColumn
                    .MaximumHeaderLength)
            .IsRequired();

        builder.Property(column =>
                column.NormalizedSourceHeader)
            .HasColumnName(
                "normalized_source_header")
            .HasMaxLength(
                CatalogImportColumn
                    .MaximumHeaderLength)
            .IsRequired();

        builder.Property(column =>
                column.TargetKind)
            .HasColumnName("target_kind")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(column =>
                column.CharacteristicDefinitionId)
            .HasColumnName(
                "characteristic_definition_id");

        builder.Property(column =>
                column.Confidence)
            .HasColumnName("confidence")
            .HasPrecision(5, 4)
            .IsRequired();

        builder.Property(column =>
                column.IsConfirmed)
            .HasColumnName("is_confirmed")
            .IsRequired();

        builder.HasOne<CatalogImportBatch>()
            .WithMany()
            .HasForeignKey(column =>
                column.BatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<CharacteristicDefinition>()
            .WithMany()
            .HasForeignKey(column =>
                column.CharacteristicDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(column =>
                new
                {
                    column.BatchId,
                    column.SourceColumnNumber
                })
            .IsUnique()
            .HasDatabaseName(
                "ux_catalog_import_columns_batch_number");

        builder.HasIndex(column =>
                new
                {
                    column.BatchId,
                    column.TargetKind
                })
            .HasDatabaseName(
                "ix_catalog_import_columns_batch_target");

        builder.HasIndex(column =>
                column.CharacteristicDefinitionId)
            .HasDatabaseName(
                "ix_catalog_import_columns_characteristic");
    }
}