using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static ElectronicService.Domain.Catalog.ImportBatches.CatalogImportBatch;

namespace ElectronicService.Infrastructure.Postgres
    .Catalog.Configurations;

public sealed class CatalogImportBatchConfiguration
    : IEntityTypeConfiguration<CatalogImportBatch>
{
    public void Configure(
        EntityTypeBuilder<CatalogImportBatch> builder)
    {
        builder.ToTable(
            "catalog_import_batches",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_catalog_import_batches_status_not_none",
                    "\"status\" <> 'None'");

                table.HasCheckConstraint(
                    "ck_catalog_import_batches_file_size",
                    "\"file_size_bytes\" > 0 " +
                    $"AND \"file_size_bytes\" <= " +
                    $"{CatalogImportBatch.MaximumFileSizeBytes}");

                table.HasCheckConstraint(
                    "ck_catalog_import_batches_rows_count",
                    "\"rows_count\" >= 0");

                table.HasCheckConstraint(
                    "ck_catalog_import_batches_valid_rows_count",
                    "\"valid_rows_count\" >= 0");

                table.HasCheckConstraint(
                    "ck_catalog_import_batches_error_rows_count",
                    "\"error_rows_count\" >= 0");

                table.HasCheckConstraint(
                    "ck_catalog_import_batches_rows_statistics",
                    "\"valid_rows_count\" + " +
                    "\"error_rows_count\" <= " +
                    "\"rows_count\"");

                table.HasCheckConstraint(
                    "ck_catalog_import_batches_file_sha256",
                    "char_length(\"file_sha256\") = 64");
            });

        builder.HasKey(batch => batch.Id);

        builder.Property(batch => batch.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(batch =>
                batch.CreatedByUserId)
            .HasColumnName(
                "created_by_user_id")
            .IsRequired();

        builder.Property(batch =>
                batch.ProductTypeId)
            .HasColumnName(
                "product_type_id");

        builder.Property(batch =>
                batch.OriginalFileName)
            .HasColumnName(
                "original_file_name")
            .HasMaxLength(
                CatalogImportBatch
                    .MaximumFileNameLength)
            .IsRequired();

        builder.Property(batch =>
                batch.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(
                CatalogImportBatch
                    .MaximumContentTypeLength)
            .IsRequired();

        builder.Property(batch =>
                batch.FileSizeBytes)
            .HasColumnName(
                "file_size_bytes")
            .IsRequired();

        builder.Property(batch =>
                batch.FileSha256)
            .HasColumnName("file_sha256")
            .HasMaxLength(64)
            .IsFixedLength()
            .IsRequired();

        builder.Property(batch =>
                batch.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(batch =>
                batch.RowsCount)
            .HasColumnName("rows_count")
            .IsRequired();

        builder.Property(batch =>
                batch.ValidRowsCount)
            .HasColumnName(
                "valid_rows_count")
            .IsRequired();

        builder.Property(batch =>
                batch.ErrorRowsCount)
            .HasColumnName(
                "error_rows_count")
            .IsRequired();

        builder.Property(batch =>
                batch.CreatedAtUtc)
            .HasColumnName(
                "created_at_utc")
            .IsRequired();

        builder.Property(batch =>
                batch.UpdatedAtUtc)
            .HasColumnName(
                "updated_at_utc");

        builder.Property(batch =>
                batch.SubmittedAtUtc)
            .HasColumnName(
                "submitted_at_utc");

        builder.Property(batch =>
                batch.ReviewedByUserId)
            .HasColumnName(
                "reviewed_by_user_id");

        builder.Property(batch =>
                batch.ReviewedAtUtc)
            .HasColumnName(
                "reviewed_at_utc");

        builder.Property(batch =>
                batch.AppliedByUserId)
            .HasColumnName(
                "applied_by_user_id");

        builder.Property(batch =>
                batch.AppliedAtUtc)
            .HasColumnName(
                "applied_at_utc");

        builder.Property(batch =>
                batch.RejectedByUserId)
            .HasColumnName(
                "rejected_by_user_id");

        builder.Property(batch =>
                batch.RejectedAtUtc)
            .HasColumnName(
                "rejected_at_utc");

        builder.Property(batch =>
                batch.RejectionReason)
            .HasColumnName(
                "rejection_reason")
            .HasMaxLength(
                CatalogImportBatch
                    .MaximumReasonLength);

        builder.Property(batch =>
                batch.FailureReason)
            .HasColumnName(
                "failure_reason")
            .HasMaxLength(
                CatalogImportBatch
                    .MaximumReasonLength);

        /*
         * PostgreSQL xmin используется для
         * optimistic concurrency.
         */
        builder.Property(batch =>
                batch.Version)
            .IsRowVersion();

        /*
         * Пользователя, создавшего batch,
         * нельзя удалить каскадно вместе
         * с импортом.
         */
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(batch =>
                batch.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(batch =>
                batch.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(batch =>
                batch.AppliedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(batch =>
                batch.RejectedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ProductType>()
            .WithMany()
            .HasForeignKey(batch =>
                batch.ProductTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        /*
         * Один batch — один исходный файл.
         */
        builder.HasOne(batch =>
                batch.File)
            .WithOne()
            .HasForeignKey<CatalogImportFile>(
                file => file.BatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(batch =>
                batch.File)
            .IsRequired();

        builder.HasIndex(batch =>
                new
                {
                    batch.CreatedByUserId,
                    batch.CreatedAtUtc
                })
            .IsDescending(
                false,
                true)
            .HasDatabaseName(
                "ix_catalog_import_batches_creator_date");

        builder.HasIndex(batch =>
                new
                {
                    batch.Status,
                    batch.CreatedAtUtc
                })
            .IsDescending(
                false,
                true)
            .HasDatabaseName(
                "ix_catalog_import_batches_status_date");

        builder.HasIndex(batch =>
                batch.FileSha256)
            .HasDatabaseName(
                "ix_catalog_import_batches_file_sha256");

        builder.HasIndex(batch =>
                batch.ProductTypeId)
            .HasDatabaseName(
                "ix_catalog_import_batches_product_type");
    }
}