using ElectronicService.Domain.Catalog.Recognition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

internal sealed class CatalogRecognitionTrainingExampleConfiguration : IEntityTypeConfiguration<CatalogRecognitionTrainingExample>
{
    public void Configure(EntityTypeBuilder<CatalogRecognitionTrainingExample> builder)
    {
        builder.ToTable("catalog_recognition_training_examples", table =>
        {
            table.HasCheckConstraint("ck_recognition_training_example_name", "char_length(btrim(\"product_name\")) > 0");
            table.HasCheckConstraint("ck_recognition_training_example_raw", "char_length(btrim(\"raw_value\")) > 0");
            table.HasCheckConstraint("ck_recognition_training_example_value", "char_length(btrim(\"normalized_value\")) > 0");
            table.HasCheckConstraint("ck_recognition_training_example_span", "\"span_start\" >= 0 AND \"span_length\" > 0");
            table.HasCheckConstraint("ck_recognition_training_example_revocation", "(\"revoked_at_utc\" IS NULL AND \"revoked_by_user_id\" IS NULL) OR (\"revoked_at_utc\" IS NOT NULL AND \"revoked_by_user_id\" IS NOT NULL AND \"revoked_at_utc\" >= \"confirmed_at_utc\")");
        });

        builder.Property(x => x.IsEvaluationOnly).HasColumnName("is_evaluation_only").HasDefaultValue(false);
        builder.HasKey(example => example.Id);
        builder.Property(example => example.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(example => example.SourceFeedbackId).HasColumnName("source_feedback_id").IsRequired();
        builder.Property(example => example.ManufacturerId).HasColumnName("manufacturer_id").IsRequired();
        builder.Property(example => example.ProductTypeId).HasColumnName("product_type_id").IsRequired();
        builder.Property(example => example.CharacteristicDefinitionId).HasColumnName("characteristic_definition_id").IsRequired();
        builder.Property(example => example.ProductName).HasColumnName("product_name").HasMaxLength(CatalogRecognitionFeedback.ProductNameMaxLength).IsRequired();
        builder.Property(example => example.RawValue).HasColumnName("raw_value").HasMaxLength(CatalogRecognitionFeedback.ProductNameMaxLength).IsRequired();
        builder.Property(example => example.SpanStart).HasColumnName("span_start").IsRequired();
        builder.Property(example => example.SpanLength).HasColumnName("span_length").IsRequired();
        builder.Property(example => example.NormalizedValue).HasColumnName("normalized_value").HasMaxLength(CatalogRecognitionFeedback.CharacteristicValueMaxLength).IsRequired();
        builder.Property(example => example.ConfirmedByUserId).HasColumnName("confirmed_by_user_id").IsRequired();
        builder.Property(example => example.ConfirmedAtUtc).HasColumnName("confirmed_at_utc").IsRequired();
        builder.Property(example => example.RevokedByUserId).HasColumnName("revoked_by_user_id");
        builder.Property(example => example.RevokedAtUtc).HasColumnName("revoked_at_utc");
        builder.Property(example => example.RevocationReason).HasColumnName("revocation_reason").HasMaxLength(1000);

        builder.HasIndex(example => example.SourceFeedbackId).IsUnique().HasFilter("\"revoked_at_utc\" IS NULL").HasDatabaseName("ux_recognition_training_example_active_source");
        builder.HasIndex(example => new { example.ManufacturerId, example.ProductTypeId, example.CharacteristicDefinitionId, example.ConfirmedAtUtc }).HasFilter("\"revoked_at_utc\" IS NULL").HasDatabaseName("ix_recognition_training_example_scope");
    }
}
