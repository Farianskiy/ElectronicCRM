using ElectronicService.Domain.Catalog.Recognition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

internal sealed class CatalogRecognitionRuleSetEntryConfiguration
    : IEntityTypeConfiguration<CatalogRecognitionRuleSetEntry>
{
    public void Configure(EntityTypeBuilder<CatalogRecognitionRuleSetEntry> builder)
    {
        builder.ToTable("catalog_recognition_rule_set_entries", table =>
        {
            table.HasCheckConstraint(
                "ck_rule_set_entry_position",
                "\"position\" >= 0 AND \"position\" < 100");

            table.HasCheckConstraint(
                "ck_rule_set_entry_kind",
                """
                (
                    "kind" = 1
                    AND "literal_draft_id" IS NOT NULL
                    AND "integer_draft_id" IS NULL
                    AND "multi_integer_draft_id" IS NULL
                )
                OR
                (
                    "kind" = 2
                    AND "literal_draft_id" IS NULL
                    AND "integer_draft_id" IS NOT NULL
                    AND "multi_integer_draft_id" IS NULL
                )
                OR
                (
                    "kind" = 3
                    AND "literal_draft_id" IS NULL
                    AND "integer_draft_id" IS NULL
                    AND "multi_integer_draft_id" IS NOT NULL
                )
                """);
        });

        builder.HasKey(entry => new { entry.VersionId, entry.Position });

        builder.Property(entry => entry.VersionId).HasColumnName("version_id");
        builder.Property(entry => entry.Position).HasColumnName("position").ValueGeneratedNever();
        builder.Property(entry => entry.Kind).HasColumnName("kind").HasConversion<int>().IsRequired();
        builder.Property(entry => entry.LiteralDraftId).HasColumnName("literal_draft_id");
        builder.Property(entry => entry.IntegerDraftId).HasColumnName("integer_draft_id");
        builder.Property(entry => entry.MultiIntegerDraftId).HasColumnName("multi_integer_draft_id");

        builder.HasOne<CatalogRecognitionLiteralDraft>()
            .WithMany()
            .HasForeignKey(entry => entry.LiteralDraftId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CatalogRecognitionIntegerDraft>()
            .WithMany()
            .HasForeignKey(entry => entry.IntegerDraftId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CatalogRecognitionMultiIntegerDraft>()
            .WithMany()
            .HasForeignKey(entry => entry.MultiIntegerDraftId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entry => new { entry.VersionId, entry.LiteralDraftId })
            .IsUnique()
            .HasFilter("\"literal_draft_id\" IS NOT NULL")
            .HasDatabaseName("ux_rule_set_entry_literal");

        builder.HasIndex(entry => new { entry.VersionId, entry.IntegerDraftId })
            .IsUnique()
            .HasFilter("\"integer_draft_id\" IS NOT NULL")
            .HasDatabaseName("ux_rule_set_entry_integer");

        builder.HasIndex(entry => new { entry.VersionId, entry.MultiIntegerDraftId })
            .IsUnique()
            .HasFilter("\"multi_integer_draft_id\" IS NOT NULL")
            .HasDatabaseName("ux_rule_set_entry_multi_integer");

        builder.HasIndex(entry => entry.LiteralDraftId)
            .HasDatabaseName("ix_rule_set_entry_literal");

        builder.HasIndex(entry => entry.IntegerDraftId)
            .HasDatabaseName("ix_rule_set_entry_integer");

        builder.HasIndex(entry => entry.MultiIntegerDraftId)
            .HasDatabaseName("ix_rule_set_entry_multi_integer");
    }
}