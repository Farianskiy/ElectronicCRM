using ElectronicService.Domain.Catalog.Recognition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

internal sealed class CatalogRecognitionCandidateEvidenceConfiguration : IEntityTypeConfiguration<CatalogRecognitionCandidateEvidence>
{
    public void Configure(EntityTypeBuilder<CatalogRecognitionCandidateEvidence> builder)
    {
        builder.ToTable("catalog_recognition_candidate_evidence");

        builder.HasKey(evidence => evidence.Id);

        builder.Property(evidence => evidence.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(evidence => evidence.CandidateId)
            .HasColumnName("candidate_id")
            .IsRequired();

        builder.Property(evidence => evidence.FeedbackId)
            .HasColumnName("feedback_id")
            .IsRequired();

        builder.Property(evidence => evidence.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasOne<CatalogRecognitionCandidate>()
            .WithMany()
            .HasForeignKey(evidence => evidence.CandidateId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_recognition_candidate_evidence_candidate");

        builder.HasOne<CatalogRecognitionFeedback>()
            .WithMany()
            .HasForeignKey(evidence => evidence.FeedbackId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_recognition_candidate_evidence_feedback");

        builder.HasIndex(evidence => evidence.FeedbackId)
            .IsUnique()
            .HasDatabaseName("ux_recognition_candidate_evidence_feedback");

        builder.HasIndex(evidence => new
        {
            evidence.CandidateId,
            evidence.CreatedAtUtc
        })
            .HasDatabaseName("ix_recognition_candidate_evidence_candidate_date");
    }
}