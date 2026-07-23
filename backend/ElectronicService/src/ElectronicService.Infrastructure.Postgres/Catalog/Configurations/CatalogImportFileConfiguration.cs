using ElectronicService.Domain.Catalog.ImportBatches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres
    .Catalog.Configurations;

public sealed class CatalogImportFileConfiguration
    : IEntityTypeConfiguration<CatalogImportFile>
{
    public void Configure(
        EntityTypeBuilder<CatalogImportFile> builder)
    {
        builder.ToTable(
            "catalog_import_files",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_catalog_import_files_content",
                    "octet_length(\"content\") > 0 " +
                    $"AND octet_length(\"content\") <= " +
                    $"{CatalogImportBatch.MaximumFileSizeBytes}");
            });

        /*
         * batch_id является одновременно
         * Primary Key и Foreign Key.
         */
        builder.HasKey(file =>
            file.BatchId);

        builder.Property(file =>
                file.BatchId)
            .HasColumnName("batch_id")
            .ValueGeneratedNever();

        /*
         * ReadOnlyMemory<byte> — публичное
         * представление содержимого.
         * Отдельную колонку для него
         * создавать не нужно.
         */
        builder.Ignore(file =>
            file.Content);

        /*
         * Маппинг закрытого свойства
         * ContentBytes на PostgreSQL bytea.
         *
         * String overload позволяет EF Core
         * найти непубличное CLR-свойство.
         */
        builder.Property<byte[]>(
                "ContentBytes")
            .HasColumnName("content")
            .HasColumnType("bytea")
            .IsRequired()
            .UsePropertyAccessMode(
                PropertyAccessMode.Property);
    }
}