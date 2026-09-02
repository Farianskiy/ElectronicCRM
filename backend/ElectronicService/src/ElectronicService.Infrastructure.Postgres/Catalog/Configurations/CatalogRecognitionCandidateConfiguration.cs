using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.Dictionaries;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Domain.Catalog.Recognition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

internal sealed class CatalogRecognitionCandidateConfiguration : IEntityTypeConfiguration<CatalogRecognitionCandidate>
{
    public void Configure(EntityTypeBuilder<CatalogRecognitionCandidate> builder)
    {
        builder.ToTable("catalog_recognition_candidates", table =>
        {
            table.HasCheckConstraint(
                "ck_catalog_recognition_candidates_phrase",
                "char_length(btrim(\"phrase\")) > 0");

            table.HasCheckConstraint(
                "ck_catalog_recognition_candidates_normalized_phrase",
                "char_length(btrim(\"normalized_phrase\")) > 0");

            table.HasCheckConstraint(
                "ck_catalog_recognition_candidates_product_type_code",
                "char_length(btrim(\"product_type_code_snapshot\")) > 0");

            table.HasCheckConstraint(
                "ck_catalog_recognition_candidates_characteristic_code",
                "char_length(btrim(\"characteristic_code_snapshot\")) > 0");

            table.HasCheckConstraint(
                "ck_catalog_recognition_candidates_proposed_value",
                "char_length(btrim(\"proposed_value\")) > 0");

            table.HasCheckConstraint(
                "ck_catalog_recognition_candidates_candidate_key",
                "\"candidate_key\" = upper(\"candidate_key\") AND char_length(\"candidate_key\") = 64");

            table.HasCheckConstraint(
                "ck_catalog_recognition_candidates_status",
                "\"status\" IN ('Accumulating', 'SuggestionCreated', 'Approved', 'Rejected')");

            table.HasCheckConstraint(
                "ck_catalog_recognition_candidates_counts",
                "\"occurrence_count\" >= 1 AND " +
                "\"accepted_count\" >= 0 AND " +
                "\"corrected_count\" >= 0 AND " +
                "\"rejected_count\" >= 0 AND " +
                "\"occurrence_count\" = \"accepted_count\" + \"corrected_count\" + \"rejected_count\"");

            table.HasCheckConstraint(
                "ck_catalog_recognition_candidates_distinct_products",
                "\"distinct_product_count\" >= 1 AND \"distinct_product_count\" <= \"occurrence_count\"");

            table.HasCheckConstraint(
                "ck_catalog_recognition_candidates_dates",
                "\"last_seen_at_utc\" >= \"first_seen_at_utc\"");

            table.HasCheckConstraint(
                "ck_catalog_recognition_candidates_suggestion_lifecycle",
                "(\"status\" = 'Accumulating' AND \"suggestion_id\" IS NULL) OR " +
                "(\"status\" IN ('SuggestionCreated', 'Approved', 'Rejected') AND \"suggestion_id\" IS NOT NULL)");
        });

        builder.HasKey(candidate => candidate.Id);

        builder.Property(candidate => candidate.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(candidate => candidate.Phrase)
            .HasColumnName("phrase")
            .HasMaxLength(CatalogRecognitionCandidate.PhraseMaxLength)
            .IsRequired();

        builder.Property(candidate => candidate.NormalizedPhrase)
            .HasColumnName("normalized_phrase")
            .HasMaxLength(CatalogRecognitionCandidate.PhraseMaxLength)
            .IsRequired();

        builder.Property(candidate => candidate.ProductTypeId)
            .HasColumnName("product_type_id")
            .IsRequired();

        builder.Property(candidate => candidate.ProductTypeCodeSnapshot)
            .HasColumnName("product_type_code_snapshot")
            .HasMaxLength(CatalogRecognitionCandidate.ProductTypeCodeMaxLength)
            .IsRequired();

        builder.Property(candidate => candidate.CharacteristicDefinitionId)
            .HasColumnName("characteristic_definition_id")
            .IsRequired();

        builder.Property(candidate => candidate.CharacteristicCodeSnapshot)
            .HasColumnName("characteristic_code_snapshot")
            .HasMaxLength(CatalogRecognitionCandidate.CharacteristicCodeMaxLength)
            .IsRequired();

        builder.Property(candidate => candidate.ProposedValue)
            .HasColumnName("proposed_value")
            .HasMaxLength(CatalogRecognitionCandidate.ProposedValueMaxLength)
            .IsRequired();

        builder.Property(candidate => candidate.CandidateKey)
            .HasColumnName("candidate_key")
            .HasMaxLength(CatalogRecognitionCandidate.CandidateKeyLength)
            .IsRequired();

        builder.Property(candidate => candidate.OccurrenceCount)
            .HasColumnName("occurrence_count")
            .IsRequired();

        builder.Property(candidate => candidate.AcceptedCount)
            .HasColumnName("accepted_count")
            .IsRequired();

        builder.Property(candidate => candidate.CorrectedCount)
            .HasColumnName("corrected_count")
            .IsRequired();

        builder.Property(candidate => candidate.RejectedCount)
            .HasColumnName("rejected_count")
            .IsRequired();

        builder.Property(candidate => candidate.DistinctProductCount)
            .HasColumnName("distinct_product_count")
            .IsRequired();

        builder.Property(candidate => candidate.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(candidate => candidate.SuggestionId)
            .HasColumnName("suggestion_id");

        builder.Property(candidate => candidate.FirstSeenAtUtc)
            .HasColumnName("first_seen_at_utc")
            .IsRequired();

        builder.Property(candidate => candidate.LastSeenAtUtc)
            .HasColumnName("last_seen_at_utc")
            .IsRequired();

        builder.Property(candidate => candidate.Version)
            .IsRowVersion();

        builder.Ignore(candidate => candidate.IsAccumulating);

        builder.Ignore(candidate => candidate.HasSuggestion);

        builder.HasOne<ProductType>()
            .WithMany()
            .HasForeignKey(candidate => candidate.ProductTypeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_recognition_candidates_product_type");

        builder.HasOne<CharacteristicDefinition>()
            .WithMany()
            .HasForeignKey(candidate => candidate.CharacteristicDefinitionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_recognition_candidates_characteristic");

        builder.HasOne<CatalogAssistantDictionarySuggestion>()
            .WithMany()
            .HasForeignKey(candidate => candidate.SuggestionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_recognition_candidates_suggestion");

        builder.HasIndex(candidate => candidate.CandidateKey)
            .IsUnique()
            .HasDatabaseName("ux_catalog_recognition_candidates_candidate_key");

        builder.HasIndex(candidate => new
        {
            candidate.ProductTypeId,
            candidate.CharacteristicDefinitionId,
            candidate.Status
        })
            .HasDatabaseName("ix_recognition_candidates_scope_status");

        builder.HasIndex(candidate => new
        {
            candidate.Status,
            candidate.LastSeenAtUtc
        })
            .IsDescending(false, true)
            .HasDatabaseName("ix_recognition_candidates_status_last_seen");

        builder.HasIndex(candidate => candidate.SuggestionId)
            .IsUnique()
            .HasFilter("\"suggestion_id\" IS NOT NULL")
            .HasDatabaseName("ux_recognition_candidates_suggestion");
    }
}