using ElectronicService.Domain.Catalog.Dictionaries;
using ElectronicService.Domain.Catalog.Manufacturers;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

internal sealed class CatalogDictionaryTermConfiguration : IEntityTypeConfiguration<CatalogDictionaryTerm>
{
    public void Configure(EntityTypeBuilder<CatalogDictionaryTerm> builder)
    {
        builder.ToTable(
            "catalog_dictionary_terms",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_catalog_dictionary_terms_status",
                    "\"status\" IN ('Pending', 'Approved', 'Rejected', 'Disabled')");

                table.HasCheckConstraint(
                    "ck_catalog_dictionary_terms_disable_reason_not_blank",
                    "\"disable_reason\" IS NULL OR char_length(btrim(\"disable_reason\")) > 0");

                table.HasCheckConstraint(
                    "ck_catalog_dictionary_terms_status_lifecycle",
                    "(\"status\" IN ('Pending', 'Rejected') " +
                    "AND \"approved_at_utc\" IS NULL " +
                    "AND \"disabled_at_utc\" IS NULL " +
                    "AND \"disabled_by_user_id\" IS NULL " +
                    "AND \"disable_reason\" IS NULL " +
                    "AND \"reactivated_at_utc\" IS NULL " +
                    "AND \"reactivated_by_user_id\" IS NULL) " +
                    "OR " +
                    "(\"status\" = 'Approved' " +
                    "AND \"approved_at_utc\" IS NOT NULL " +
                    "AND ((" +
                    "\"disabled_at_utc\" IS NULL " +
                    "AND \"disabled_by_user_id\" IS NULL " +
                    "AND \"disable_reason\" IS NULL " +
                    "AND \"reactivated_at_utc\" IS NULL " +
                    "AND \"reactivated_by_user_id\" IS NULL" +
                    ") OR (" +
                    "\"disabled_at_utc\" IS NOT NULL " +
                    "AND \"disabled_by_user_id\" IS NOT NULL " +
                    "AND \"disable_reason\" IS NOT NULL " +
                    "AND \"reactivated_at_utc\" IS NOT NULL " +
                    "AND \"reactivated_by_user_id\" IS NOT NULL" +
                    "))) " +
                    "OR " +
                    "(\"status\" = 'Disabled' " +
                    "AND \"approved_at_utc\" IS NOT NULL " +
                    "AND \"disabled_at_utc\" IS NOT NULL " +
                    "AND \"disabled_by_user_id\" IS NOT NULL " +
                    "AND \"disable_reason\" IS NOT NULL " +
                    "AND \"reactivated_at_utc\" IS NULL " +
                    "AND \"reactivated_by_user_id\" IS NULL)");

                table.HasCheckConstraint(
                    "ck_catalog_dictionary_terms_approved_after_created",
                    "\"approved_at_utc\" IS NULL OR \"approved_at_utc\" >= \"created_at_utc\"");

                table.HasCheckConstraint(
                    "ck_catalog_dictionary_terms_disabled_after_approved",
                    "\"disabled_at_utc\" IS NULL OR (\"approved_at_utc\" IS NOT NULL AND \"disabled_at_utc\" >= \"approved_at_utc\")");

                table.HasCheckConstraint(
                    "ck_catalog_dictionary_terms_reactivated_after_disabled",
                    "\"reactivated_at_utc\" IS NULL OR (\"disabled_at_utc\" IS NOT NULL AND \"reactivated_at_utc\" >= \"disabled_at_utc\")");
            });

        builder.HasKey(term => term.Id);

        builder.Property(term => term.Id)
            .HasColumnName("id");

        builder.Property(term => term.ManufacturerId)
            .HasColumnName("manufacturer_id");

        builder.Property(term => term.ProductTypeId)
            .HasColumnName("product_type_id");

        builder.Property(term => term.Phrase)
            .HasColumnName("phrase")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(term => term.NormalizedPhrase)
            .HasColumnName("normalized_phrase")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(term => term.Kind)
            .HasColumnName("kind")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(term => term.TargetCode)
            .HasColumnName("target_code")
            .HasMaxLength(100);

        builder.Property(term => term.TargetValue)
            .HasColumnName("target_value")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(term => term.Priority)
            .HasColumnName("priority")
            .IsRequired();

        builder.Property(term => term.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(term => term.Source)
            .HasColumnName("source")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(term => term.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(term => term.ApprovedAtUtc)
            .HasColumnName("approved_at_utc");

        builder.Property(term => term.DisabledAtUtc)
            .HasColumnName("disabled_at_utc");

        builder.Property(term => term.DisabledByUserId)
            .HasColumnName("disabled_by_user_id");

        builder.Property(term => term.DisableReason)
            .HasColumnName("disable_reason")
            .HasMaxLength(CatalogDictionaryTerm.DisableReasonMaxLength);

        builder.Property(term => term.ReactivatedAtUtc)
            .HasColumnName("reactivated_at_utc");

        builder.Property(term => term.ReactivatedByUserId)
            .HasColumnName("reactivated_by_user_id");

        builder.HasOne<Manufacturer>()
            .WithMany()
            .HasForeignKey(term => term.ManufacturerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_catalog_dictionary_terms_manufacturer");

        builder.HasOne<ProductType>()
            .WithMany()
            .HasForeignKey(term => term.ProductTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(term => term.DisabledByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(term => term.ReactivatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(term => term.NormalizedPhrase)
            .HasDatabaseName("ix_catalog_dictionary_terms_normalized_phrase");

        builder.HasIndex(term => term.Status)
            .HasDatabaseName("ix_catalog_dictionary_terms_status");

        builder.HasIndex(term => term.Source)
            .HasDatabaseName("ix_catalog_dictionary_terms_source");

        builder.HasIndex(term => term.DisabledByUserId)
            .HasDatabaseName("ix_catalog_dictionary_terms_disabled_by_user_id");

        builder.HasIndex(term => term.ReactivatedByUserId)
            .HasDatabaseName("ix_catalog_dictionary_terms_reactivated_by_user_id");

        builder.HasIndex(term => new
        {
            term.ManufacturerId,
            term.ProductTypeId,
            term.Status
        })
            .HasDatabaseName("ix_catalog_dictionary_terms_scope_status");

        builder.HasIndex(term => new
        {
            term.ManufacturerId,
            term.ProductTypeId,
            term.NormalizedPhrase,
            term.Kind,
            term.TargetCode,
            term.TargetValue
        })
            .IsUnique()
            .AreNullsDistinct(false)
            .HasDatabaseName("ux_catalog_dictionary_terms_scope_mapping");
    }
}