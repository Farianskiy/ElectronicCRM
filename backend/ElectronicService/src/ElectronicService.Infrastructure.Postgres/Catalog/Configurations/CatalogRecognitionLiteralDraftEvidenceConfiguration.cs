using ElectronicService.Domain.Catalog.Recognition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

internal sealed class CatalogRecognitionLiteralDraftEvidenceConfiguration : IEntityTypeConfiguration<CatalogRecognitionLiteralDraftEvidence>
{
    public void Configure(EntityTypeBuilder<CatalogRecognitionLiteralDraftEvidence> builder)
    {
        builder.ToTable("catalog_recognition_literal_draft_evidence");

        builder.HasKey(evidence => new { evidence.DraftId, evidence.TrainingExampleId });
        builder.Property(evidence => evidence.DraftId).HasColumnName("draft_id").IsRequired();
        builder.Property(evidence => evidence.TrainingExampleId).HasColumnName("training_example_id").IsRequired();
        builder.Property(evidence => evidence.IsSupporting).HasColumnName("is_supporting").IsRequired();

        builder.HasOne<CatalogRecognitionTrainingExample>().WithMany().HasForeignKey(evidence => evidence.TrainingExampleId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(evidence => evidence.TrainingExampleId).HasDatabaseName("ix_literal_draft_evidence_example");
    }
}