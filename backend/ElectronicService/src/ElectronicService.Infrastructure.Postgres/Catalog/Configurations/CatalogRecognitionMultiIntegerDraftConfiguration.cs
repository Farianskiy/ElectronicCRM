using ElectronicService.Domain.Catalog.Recognition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

internal sealed class CatalogRecognitionMultiIntegerDraftConfiguration : IEntityTypeConfiguration<CatalogRecognitionMultiIntegerDraft>
{
    public void Configure(EntityTypeBuilder<CatalogRecognitionMultiIntegerDraft> builder)
    {
        builder.ToTable("catalog_recognition_multi_integer_drafts", table =>
        {
            table.HasCheckConstraint("ck_multi_integer_draft_generator", "char_length(btrim(\"generator_version\")) > 0");
            table.HasCheckConstraint("ck_multi_integer_draft_counts", "\"supporting_name_count\" >= 2 AND \"matched_name_count\" = \"supporting_name_count\" AND \"matched_name_count\" <= 200");
        });

        builder.HasKey(draft => draft.Id);
        builder.Property(draft => draft.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(draft => draft.ManufacturerId).HasColumnName("manufacturer_id").IsRequired();
        builder.Property(draft => draft.ProductTypeId).HasColumnName("product_type_id").IsRequired();
        builder.Property(draft => draft.GeneratorVersion).HasColumnName("generator_version").HasMaxLength(100).IsRequired();
        builder.Property(draft => draft.MatchedNameCount).HasColumnName("matched_name_count").IsRequired();
        builder.Property(draft => draft.SupportingNameCount).HasColumnName("supporting_name_count").IsRequired();
        builder.Property(draft => draft.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(draft => draft.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasMany(draft => draft.Parts).WithOne().HasForeignKey(part => part.DraftId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(draft => draft.Parts).HasField("_parts").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(draft => draft.Evidence).WithOne().HasForeignKey(evidence => evidence.DraftId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(draft => draft.Evidence).HasField("_evidence").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(draft => new { draft.ManufacturerId, draft.ProductTypeId, draft.CreatedAtUtc }).HasDatabaseName("ix_multi_integer_draft_scope");
    }
}