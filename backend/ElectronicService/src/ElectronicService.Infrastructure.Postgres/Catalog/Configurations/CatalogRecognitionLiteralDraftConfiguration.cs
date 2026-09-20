using ElectronicService.Domain.Catalog.Recognition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

internal sealed class CatalogRecognitionLiteralDraftConfiguration : IEntityTypeConfiguration<CatalogRecognitionLiteralDraft>
{
    public void Configure(EntityTypeBuilder<CatalogRecognitionLiteralDraft> builder)
    {
        builder.ToTable("catalog_recognition_literal_drafts", table =>
        {
            table.HasCheckConstraint("ck_literal_draft_literal", "char_length(btrim(\"literal\")) > 0");
            table.HasCheckConstraint("ck_literal_draft_value", "char_length(btrim(\"normalized_value\")) > 0");
            table.HasCheckConstraint("ck_literal_draft_generator", "char_length(btrim(\"generator_version\")) > 0");
            table.HasCheckConstraint("ck_literal_draft_matched_names", "\"matched_name_count\" > 0");
        });

        builder.HasKey(draft => draft.Id);
        builder.Property(draft => draft.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(draft => draft.ManufacturerId).HasColumnName("manufacturer_id").IsRequired();
        builder.Property(draft => draft.ProductTypeId).HasColumnName("product_type_id").IsRequired();
        builder.Property(draft => draft.CharacteristicDefinitionId).HasColumnName("characteristic_definition_id").IsRequired();
        builder.Property(draft => draft.Literal).HasColumnName("literal").HasMaxLength(2000).IsRequired();
        builder.Property(draft => draft.NormalizedValue).HasColumnName("normalized_value").HasMaxLength(2000).IsRequired();
        builder.Property(draft => draft.GeneratorVersion).HasColumnName("generator_version").HasMaxLength(100).IsRequired();
        builder.Property(draft => draft.MatchedNameCount).HasColumnName("matched_name_count").IsRequired();
        builder.Property(draft => draft.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(draft => draft.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasMany(draft => draft.Evidence).WithOne().HasForeignKey(evidence => evidence.DraftId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(draft => draft.Evidence).HasField("_evidence").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(draft => new { draft.ManufacturerId, draft.ProductTypeId, draft.CharacteristicDefinitionId, draft.CreatedAtUtc }).HasDatabaseName("ix_literal_draft_scope");
    }
}