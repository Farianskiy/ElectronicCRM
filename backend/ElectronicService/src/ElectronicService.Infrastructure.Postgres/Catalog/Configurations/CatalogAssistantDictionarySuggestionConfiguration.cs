using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.Dictionaries;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

internal sealed class CatalogAssistantDictionarySuggestionConfiguration : IEntityTypeConfiguration<CatalogAssistantDictionarySuggestion>
{
    public void Configure(EntityTypeBuilder<CatalogAssistantDictionarySuggestion> builder)
    {
        builder.ToTable("catalog_assistant_dictionary_suggestions", table =>
        {
            table.HasCheckConstraint(
                "ck_dictionary_suggestions_source",
                "\"source\" IN ('Assistant', 'ImportRecognition', 'UserCorrection', 'RecognitionLearning', 'MlRecognition')");

            table.HasCheckConstraint(
                "ck_dictionary_suggestions_evidence_counts",
                "\"occurrence_count\" > 0 AND " +
                "\"accepted_evidence_count\" >= 0 AND " +
                "\"corrected_evidence_count\" >= 0 AND " +
                "\"rejected_evidence_count\" >= 0 AND " +
                "\"accepted_evidence_count\"::bigint + \"corrected_evidence_count\"::bigint + \"rejected_evidence_count\"::bigint <= \"occurrence_count\"::bigint");

            table.HasCheckConstraint(
                "ck_dictionary_suggestions_characteristic_scope",
                "\"characteristic_definition_id\" IS NULL OR (\"product_type_id\" IS NOT NULL AND \"suggested_kind\" = 'Characteristic')");

            table.HasCheckConstraint(
                "ck_dictionary_suggestions_generated_source",
                "NOT (\"source\" = 'Assistant' AND \"generated_automatically\" = TRUE)");

            table.HasCheckConstraint(
                "ck_dictionary_suggestions_recognition_learning",
                "\"source\" <> 'RecognitionLearning' OR (\"generated_automatically\" = TRUE AND \"product_type_id\" IS NOT NULL AND \"characteristic_definition_id\" IS NOT NULL AND \"suggested_kind\" = 'Characteristic')");

            table.HasCheckConstraint(
                "ck_dictionary_suggestions_approved_kind",
                "\"approved_kind\" IS NULL OR \"approved_kind\" IN ('Manufacturer', 'ProductType', 'Characteristic', 'SearchToken')");

            table.HasCheckConstraint(
                "ck_dictionary_suggestions_approved_priority",
                "\"approved_priority\" IS NULL OR (\"approved_priority\" >= 1 AND \"approved_priority\" <= 10000)");

            table.HasCheckConstraint(
                "ck_dictionary_suggestions_approved_characteristic_scope",
                "\"approved_characteristic_definition_id\" IS NULL OR (\"approved_product_type_id\" IS NOT NULL AND \"approved_kind\" = 'Characteristic')");

            table.HasCheckConstraint(
                "ck_dictionary_suggestions_approved_decision",
                "(\"approved_phrase\" IS NULL AND " +
                "\"approved_kind\" IS NULL AND " +
                "\"approved_target_code\" IS NULL AND " +
                "\"approved_target_value\" IS NULL AND " +
                "\"approved_product_type_id\" IS NULL AND " +
                "\"approved_characteristic_definition_id\" IS NULL AND " +
                "\"approved_priority\" IS NULL AND " +
                "\"created_dictionary_term_id\" IS NULL) OR " +
                "(\"approved_phrase\" IS NOT NULL AND " +
                "\"approved_kind\" IS NOT NULL AND " +
                "\"approved_target_value\" IS NOT NULL AND " +
                "\"approved_priority\" IS NOT NULL AND " +
                "\"created_dictionary_term_id\" IS NOT NULL)");
        });

        builder.HasKey(suggestion => suggestion.Id);

        builder.Property(suggestion => suggestion.Id)
            .HasColumnName("id");

        builder.Property(suggestion => suggestion.OriginalMessage)
            .HasColumnName("original_message")
            .HasMaxLength(CatalogAssistantDictionarySuggestion.OriginalMessageMaxLength)
            .IsRequired();

        builder.Property(suggestion => suggestion.UnknownPhrase)
            .HasColumnName("unknown_phrase")
            .HasMaxLength(CatalogAssistantDictionarySuggestion.UnknownPhraseMaxLength)
            .IsRequired();

        builder.Property(suggestion => suggestion.NormalizedUnknownPhrase)
            .HasColumnName("normalized_unknown_phrase")
            .HasMaxLength(CatalogAssistantDictionarySuggestion.UnknownPhraseMaxLength)
            .IsRequired();

        builder.Property(suggestion => suggestion.SuggestedKind)
            .HasColumnName("suggested_kind")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(suggestion => suggestion.SuggestedTargetCode)
            .HasColumnName("suggested_target_code")
            .HasMaxLength(CatalogAssistantDictionarySuggestion.SuggestedTargetCodeMaxLength);

        builder.Property(suggestion => suggestion.SuggestedTargetValue)
            .HasColumnName("suggested_target_value")
            .HasMaxLength(CatalogAssistantDictionarySuggestion.SuggestedTargetValueMaxLength)
            .IsRequired();

        builder.Property(suggestion => suggestion.Confidence)
            .HasColumnName("confidence")
            .HasPrecision(5, 4)
            .IsRequired();

        builder.Property(suggestion => suggestion.Source)
            .HasColumnName("source")
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasSentinel(CatalogDictionarySuggestionSource.None)
            .HasDefaultValue(CatalogDictionarySuggestionSource.Assistant)
            .IsRequired();

        builder.Property(suggestion => suggestion.ProductTypeId)
            .HasColumnName("product_type_id");

        builder.Property(suggestion => suggestion.CharacteristicDefinitionId)
            .HasColumnName("characteristic_definition_id");

        builder.Property(suggestion => suggestion.OccurrenceCount)
            .HasColumnName("occurrence_count")
            .HasDefaultValue(1)
            .IsRequired();

        builder.Property(suggestion => suggestion.AcceptedEvidenceCount)
            .HasColumnName("accepted_evidence_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(suggestion => suggestion.CorrectedEvidenceCount)
            .HasColumnName("corrected_evidence_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(suggestion => suggestion.RejectedEvidenceCount)
            .HasColumnName("rejected_evidence_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(suggestion => suggestion.GeneratedAutomatically)
            .HasColumnName("generated_automatically")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(suggestion => suggestion.ApprovedPhrase)
            .HasColumnName("approved_phrase")
            .HasMaxLength(CatalogAssistantDictionarySuggestion.UnknownPhraseMaxLength);

        builder.Property(suggestion => suggestion.ApprovedKind)
            .HasColumnName("approved_kind")
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(suggestion => suggestion.ApprovedTargetCode)
            .HasColumnName("approved_target_code")
            .HasMaxLength(CatalogAssistantDictionarySuggestion.SuggestedTargetCodeMaxLength);

        builder.Property(suggestion => suggestion.ApprovedTargetValue)
            .HasColumnName("approved_target_value")
            .HasMaxLength(CatalogAssistantDictionarySuggestion.SuggestedTargetValueMaxLength);

        builder.Property(suggestion => suggestion.ApprovedProductTypeId)
            .HasColumnName("approved_product_type_id");

        builder.Property(suggestion => suggestion.ApprovedCharacteristicDefinitionId)
            .HasColumnName("approved_characteristic_definition_id");

        builder.Property(suggestion => suggestion.ApprovedPriority)
            .HasColumnName("approved_priority");

        builder.Property(suggestion => suggestion.CreatedDictionaryTermId)
            .HasColumnName("created_dictionary_term_id");

        builder.Property(suggestion => suggestion.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(suggestion => suggestion.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(suggestion => suggestion.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(suggestion => suggestion.ReviewedByUserId)
            .HasColumnName("reviewed_by_user_id");

        builder.Property(suggestion => suggestion.ReviewedAtUtc)
            .HasColumnName("reviewed_at_utc");

        builder.Property(suggestion => suggestion.ReviewComment)
            .HasColumnName("review_comment")
            .HasMaxLength(CatalogAssistantDictionarySuggestion.ReviewCommentMaxLength);

        builder.Ignore(suggestion => suggestion.IsPending);

        builder.Ignore(suggestion => suggestion.IsApproved);

        builder.Ignore(suggestion => suggestion.IsRejected);

        builder.Ignore(suggestion => suggestion.IsScopedToProductType);

        builder.Ignore(suggestion => suggestion.IsGeneratedFromRecognitionLearning);

        builder.HasOne<ProductType>()
            .WithMany()
            .HasForeignKey(suggestion => suggestion.ProductTypeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_dictionary_suggestions_product_type");

        builder.HasOne<CharacteristicDefinition>()
            .WithMany()
            .HasForeignKey(suggestion => suggestion.CharacteristicDefinitionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_dictionary_suggestions_characteristic");

        builder.HasOne<ProductType>()
            .WithMany()
            .HasForeignKey(suggestion => suggestion.ApprovedProductTypeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_dictionary_suggestions_approved_product_type");

        builder.HasOne<CharacteristicDefinition>()
            .WithMany()
            .HasForeignKey(suggestion => suggestion.ApprovedCharacteristicDefinitionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_dictionary_suggestions_approved_characteristic");

        builder.HasOne<CatalogDictionaryTerm>()
            .WithMany()
            .HasForeignKey(suggestion => suggestion.CreatedDictionaryTermId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_dictionary_suggestions_created_term");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(suggestion => suggestion.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(suggestion => suggestion.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(suggestion => suggestion.Status)
            .HasDatabaseName("ix_catalog_assistant_dictionary_suggestions_status");

        builder.HasIndex(suggestion => suggestion.NormalizedUnknownPhrase)
            .HasDatabaseName("ix_catalog_assistant_dictionary_suggestions_normalized_unknown_phrase");

        builder.HasIndex(suggestion => suggestion.CreatedAtUtc)
            .HasDatabaseName("ix_catalog_assistant_dictionary_suggestions_created_at_utc");

        builder.HasIndex(suggestion => suggestion.CreatedByUserId)
            .HasDatabaseName("ix_catalog_assistant_dictionary_suggestions_created_by_user_id");

        builder.HasIndex(suggestion => suggestion.ReviewedByUserId)
            .HasDatabaseName("ix_catalog_assistant_dictionary_suggestions_reviewed_by_user_id");

        builder.HasIndex(suggestion => suggestion.CharacteristicDefinitionId)
            .HasDatabaseName("ix_dictionary_suggestions_characteristic");

        builder.HasIndex(suggestion => new
        {
            suggestion.Source,
            suggestion.Status,
            suggestion.CreatedAtUtc
        })
            .HasDatabaseName("ix_dictionary_suggestions_source_status_created");

        builder.HasIndex(suggestion => new
        {
            suggestion.ProductTypeId,
            suggestion.CharacteristicDefinitionId,
            suggestion.Status
        })
            .HasDatabaseName("ix_dictionary_suggestions_scope_status");

        builder.HasIndex(suggestion => suggestion.CreatedDictionaryTermId)
            .IsUnique()
            .HasFilter("\"created_dictionary_term_id\" IS NOT NULL")
            .HasDatabaseName("ux_dictionary_suggestions_created_term");

        builder.HasIndex(suggestion => new
        {
            suggestion.ApprovedProductTypeId,
            suggestion.ApprovedCharacteristicDefinitionId
        })
            .HasDatabaseName("ix_dictionary_suggestions_approved_scope");
    }
}