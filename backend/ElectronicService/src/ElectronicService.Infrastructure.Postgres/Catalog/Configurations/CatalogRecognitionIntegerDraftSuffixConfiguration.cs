using ElectronicService.Domain.Catalog.Recognition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

internal sealed class CatalogRecognitionIntegerDraftSuffixConfiguration : IEntityTypeConfiguration<CatalogRecognitionIntegerDraftSuffix>
{
    public void Configure(EntityTypeBuilder<CatalogRecognitionIntegerDraftSuffix> builder)
    {
        builder.ToTable("catalog_recognition_integer_draft_suffixes", table =>
        {
            table.HasCheckConstraint("ck_integer_draft_suffix_position", "\"position\" >= 0 AND \"position\" < 16");
        });

        builder.HasKey(suffix => new { suffix.DraftId, suffix.Position });
        builder.Property(suffix => suffix.DraftId).HasColumnName("draft_id").IsRequired();
        builder.Property(suffix => suffix.Position).HasColumnName("position").ValueGeneratedNever();
        builder.Property(suffix => suffix.Text).HasColumnName("text").HasMaxLength(2000).IsRequired();
    }
}