using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.Dictionaries;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

internal sealed class CatalogRecognitionFeedbackConfiguration : IEntityTypeConfiguration<CatalogRecognitionFeedback>
{
    public void Configure(EntityTypeBuilder<CatalogRecognitionFeedback> builder)
    {
        builder.ToTable(
            "catalog_recognition_feedback",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_catalog_recognition_feedback_product_name",
                    "char_length(btrim(\"product_name\")) > 0");

                table.HasCheckConstraint(
                    "ck_catalog_recognition_feedback_normalized_product_name",
                    "char_length(btrim(\"normalized_product_name\")) > 0");

                table.HasCheckConstraint(
                    "ck_catalog_recognition_feedback_product_type_code",
                    "char_length(btrim(\"product_type_code_snapshot\")) > 0");

                table.HasCheckConstraint(
                    "ck_catalog_recognition_feedback_characteristic_code",
                    "char_length(btrim(\"characteristic_code_snapshot\")) > 0");

                table.HasCheckConstraint(
                    "ck_catalog_recognition_feedback_status",
                    "\"status\" IN ('Pending', 'Finalized')");

                table.HasCheckConstraint(
                    "ck_catalog_recognition_feedback_type",
                    "\"feedback_type\" IN ('None', 'Accepted', 'Corrected', 'Rejected', 'AddedManually', 'ConflictResolved')");

                table.HasCheckConstraint(
                    "ck_catalog_recognition_feedback_label_quality",
                    "\"label_quality\" IN ('None', 'Weak', 'Medium', 'Strong')");

                table.HasCheckConstraint(
                    "ck_catalog_recognition_feedback_confidence",
                    "\"suggested_confidence\" IS NULL OR (\"suggested_confidence\" >= 0 AND \"suggested_confidence\" <= 1)");

                table.HasCheckConstraint(
                    "ck_catalog_recognition_feedback_suggested_evidence",
                    "(\"suggested_raw_value\" IS NULL AND \"suggested_normalized_value\" IS NULL AND \"suggested_source\" IS NULL) OR " +
                    "(\"suggested_raw_value\" IS NOT NULL AND char_length(btrim(\"suggested_raw_value\")) > 0 AND " +
                    "\"suggested_normalized_value\" IS NOT NULL AND char_length(btrim(\"suggested_normalized_value\")) > 0 AND " +
                    "\"suggested_source\" IS NOT NULL AND char_length(btrim(\"suggested_source\")) > 0)");

                table.HasCheckConstraint(
                    "ck_catalog_recognition_feedback_confidence_evidence",
                    "\"suggested_confidence\" IS NULL OR \"suggested_normalized_value\" IS NOT NULL");

                table.HasCheckConstraint(
                    "ck_catalog_recognition_feedback_span",
                    "(\"span_start\" IS NULL AND \"span_length\" IS NULL) OR " +
                    "(\"span_start\" IS NOT NULL AND \"span_start\" >= 0 AND " +
                    "\"span_length\" IS NOT NULL AND \"span_length\" > 0 AND " +
                    "\"suggested_raw_value\" IS NOT NULL AND " +
                    "\"span_start\" + \"span_length\" <= char_length(\"product_name\"))");

                table.HasCheckConstraint(
                    "ck_catalog_recognition_feedback_import_links",
                    "\"import_row_id\" IS NULL OR \"import_batch_id\" IS NOT NULL");

                table.HasCheckConstraint(
                    "ck_catalog_recognition_feedback_final_value",
                    "(\"feedback_type\" IN ('None', 'Rejected') AND \"final_normalized_value\" IS NULL) OR " +
                    "(\"feedback_type\" IN ('Accepted', 'Corrected', 'AddedManually', 'ConflictResolved') AND " +
                    "\"final_normalized_value\" IS NOT NULL AND char_length(btrim(\"final_normalized_value\")) > 0)");

                table.HasCheckConstraint(
                    "ck_catalog_recognition_feedback_accepted_value",
                    "\"feedback_type\" <> 'Accepted' OR " +
                    "(\"suggested_normalized_value\" IS NOT NULL AND \"final_normalized_value\" = \"suggested_normalized_value\")");

                table.HasCheckConstraint(
                    "ck_catalog_recognition_feedback_corrected_value",
                    "\"feedback_type\" <> 'Corrected' OR " +
                    "(\"suggested_normalized_value\" IS NOT NULL AND \"final_normalized_value\" <> \"suggested_normalized_value\")");

                table.HasCheckConstraint(
                    "ck_catalog_recognition_feedback_added_manually",
                    "\"feedback_type\" <> 'AddedManually' OR \"suggested_normalized_value\" IS NULL");

                table.HasCheckConstraint(
                    "ck_catalog_recognition_feedback_reviewer_role",
                    "\"reviewer_role\" IS NULL OR char_length(btrim(\"reviewer_role\")) > 0");

                table.HasCheckConstraint(
                    "ck_catalog_recognition_feedback_model_version",
                    "\"model_version\" IS NULL OR char_length(btrim(\"model_version\")) > 0");

                table.HasCheckConstraint(
                    "ck_catalog_recognition_feedback_lifecycle",
                    "(\"status\" = 'Pending' AND " +
                    "\"feedback_type\" <> 'None' AND " +
                    "\"label_quality\" = 'None' AND " +
                    "\"reviewed_by_user_id\" IS NULL AND " +
                    "\"reviewer_role\" IS NULL AND " +
                    "\"finalized_at_utc\" IS NULL AND " +
                    "\"is_training_eligible\" = FALSE) OR " +
                    "(\"status\" = 'Finalized' AND " +
                    "\"feedback_type\" <> 'None' AND " +
                    "\"label_quality\" <> 'None' AND " +
                    "\"reviewed_by_user_id\" IS NOT NULL AND " +
                    "\"reviewer_role\" IS NOT NULL AND " +
                    "\"finalized_at_utc\" IS NOT NULL)");

                table.HasCheckConstraint(
                    "ck_catalog_recognition_feedback_training_quality",
                    "\"is_training_eligible\" = FALSE OR " +
                    "(\"status\" = 'Finalized' AND \"label_quality\" IN ('Medium', 'Strong'))");

                table.HasCheckConstraint(
                    "ck_catalog_recognition_feedback_finalized_date",
                    "\"finalized_at_utc\" IS NULL OR \"finalized_at_utc\" >= \"created_at_utc\"");
            });

        builder.HasKey(feedback => feedback.Id);

        builder.Property(feedback => feedback.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(feedback => feedback.ProductName)
            .HasColumnName("product_name")
            .HasMaxLength(CatalogRecognitionFeedback.ProductNameMaxLength)
            .IsRequired();

        builder.Property(feedback => feedback.NormalizedProductName)
            .HasColumnName("normalized_product_name")
            .HasMaxLength(CatalogRecognitionFeedback.ProductNameMaxLength)
            .IsRequired();

        builder.Property(feedback => feedback.ProductTypeId)
            .HasColumnName("product_type_id")
            .IsRequired();

        builder.Property(feedback => feedback.ProductTypeCodeSnapshot)
            .HasColumnName("product_type_code_snapshot")
            .HasMaxLength(CatalogRecognitionFeedback.ProductTypeCodeMaxLength)
            .IsRequired();

        builder.Property(feedback => feedback.CharacteristicDefinitionId)
            .HasColumnName("characteristic_definition_id")
            .IsRequired();

        builder.Property(feedback => feedback.CharacteristicCodeSnapshot)
            .HasColumnName("characteristic_code_snapshot")
            .HasMaxLength(CatalogRecognitionFeedback.CharacteristicCodeMaxLength)
            .IsRequired();

        builder.Property(feedback => feedback.SuggestedRawValue)
            .HasColumnName("suggested_raw_value")
            .HasMaxLength(CatalogRecognitionFeedback.CharacteristicValueMaxLength);

        builder.Property(feedback => feedback.SuggestedNormalizedValue)
            .HasColumnName("suggested_normalized_value")
            .HasMaxLength(CatalogRecognitionFeedback.CharacteristicValueMaxLength);

        builder.Property(feedback => feedback.SuggestedConfidence)
            .HasColumnName("suggested_confidence")
            .HasPrecision(5, 4);

        builder.Property(feedback => feedback.SuggestedSource)
            .HasColumnName("suggested_source")
            .HasMaxLength(CatalogRecognitionFeedback.SuggestedSourceMaxLength);

        builder.Property(feedback => feedback.SpanStart)
            .HasColumnName("span_start");

        builder.Property(feedback => feedback.SpanLength)
            .HasColumnName("span_length");

        builder.Property(feedback => feedback.FinalNormalizedValue)
            .HasColumnName("final_normalized_value")
            .HasMaxLength(CatalogRecognitionFeedback.CharacteristicValueMaxLength);

        builder.Property(feedback => feedback.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(feedback => feedback.FeedbackType)
            .HasColumnName("feedback_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(feedback => feedback.LabelQuality)
            .HasColumnName("label_quality")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(feedback => feedback.DictionaryTermId)
            .HasColumnName("dictionary_term_id");

        builder.Property(feedback => feedback.RecognitionProfileId)
            .HasColumnName("recognition_profile_id");

        builder.Property(feedback => feedback.ModelVersion)
            .HasColumnName("model_version")
            .HasMaxLength(CatalogRecognitionFeedback.ModelVersionMaxLength);

        builder.Property(feedback => feedback.ImportBatchId)
            .HasColumnName("import_batch_id");

        builder.Property(feedback => feedback.ImportRowId)
            .HasColumnName("import_row_id");

        builder.Property(feedback => feedback.ReviewedByUserId)
            .HasColumnName("reviewed_by_user_id");

        builder.Property(feedback => feedback.ReviewerRole)
            .HasColumnName("reviewer_role")
            .HasMaxLength(CatalogRecognitionFeedback.ReviewerRoleMaxLength);

        builder.Property(feedback => feedback.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(feedback => feedback.FinalizedAtUtc)
            .HasColumnName("finalized_at_utc");

        builder.Property(feedback => feedback.IsTrainingEligible)
            .HasColumnName("is_training_eligible")
            .IsRequired();

        builder.Ignore(feedback => feedback.IsPending);

        builder.Ignore(feedback => feedback.IsFinalized);

        builder.HasOne<ProductType>()
            .WithMany()
            .HasForeignKey(feedback => feedback.ProductTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CharacteristicDefinition>()
            .WithMany()
            .HasForeignKey(feedback => feedback.CharacteristicDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CatalogDictionaryTerm>()
            .WithMany()
            .HasForeignKey(feedback => feedback.DictionaryTermId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<CatalogCharacteristicRecognitionProfile>()
            .WithMany()
            .HasForeignKey(feedback => feedback.RecognitionProfileId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<CatalogImportBatch>()
            .WithMany()
            .HasForeignKey(feedback => feedback.ImportBatchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<CatalogImportRow>()
            .WithMany()
            .HasForeignKey(feedback => feedback.ImportRowId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(feedback => feedback.ReviewedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(feedback => new
        {
            feedback.ImportRowId,
            feedback.CharacteristicDefinitionId
        })
            .IsUnique()
            .HasFilter("\"import_row_id\" IS NOT NULL")
            .HasDatabaseName("ux_catalog_recognition_feedback_import_row_characteristic");

        builder.HasIndex(feedback => new
        {
            feedback.ProductTypeId,
            feedback.CharacteristicDefinitionId,
            feedback.FinalizedAtUtc
        })
            .HasFilter("\"status\" = 'Finalized'")
            .HasDatabaseName("ix_catalog_recognition_feedback_candidate_scope");

        builder.HasIndex(feedback => feedback.FinalizedAtUtc)
            .HasFilter("\"status\" = 'Finalized' AND \"is_training_eligible\" = TRUE")
            .HasDatabaseName("ix_catalog_recognition_feedback_training_export");

        builder.HasIndex(feedback => new
        {
            feedback.FeedbackType,
            feedback.FinalizedAtUtc
        })
            .HasFilter("\"status\" = 'Finalized'")
            .HasDatabaseName("ix_catalog_recognition_feedback_type_date");

        builder.HasIndex(feedback => new
        {
            feedback.ImportBatchId,
            feedback.ImportRowId
        })
            .HasDatabaseName("ix_catalog_recognition_feedback_import_source");

        builder.HasIndex(feedback => feedback.DictionaryTermId)
            .HasDatabaseName("ix_catalog_recognition_feedback_dictionary_term");

        builder.HasIndex(feedback => feedback.RecognitionProfileId)
            .HasDatabaseName("ix_catalog_recognition_feedback_recognition_profile");

        builder.HasIndex(feedback => feedback.ReviewedByUserId)
            .HasDatabaseName("ix_catalog_recognition_feedback_reviewer");
    }
}