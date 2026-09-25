using ElectronicService.Domain.Catalog.Dictionaries;
using ElectronicService.Domain.Catalog.Recognition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

internal sealed class CatalogDictionaryEvaluationReportConfiguration : IEntityTypeConfiguration<CatalogDictionaryEvaluationReport>
{
    public void Configure(EntityTypeBuilder<CatalogDictionaryEvaluationReport> b)
    {
        b.ToTable("catalog_dictionary_evaluation_reports", t => t.HasCheckConstraint("ck_dictionary_evaluation_snapshot", "jsonb_typeof(snapshot_json) = 'object' AND octet_length(snapshot_json::text) <= 10000000"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        b.Property(x => x.SuggestionId).HasColumnName("suggestion_id");
        b.Property(x => x.CandidateId).HasColumnName("candidate_id");
        b.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.SnapshotJson).HasColumnName("snapshot_json").HasColumnType("jsonb").IsRequired();
        b.HasOne<CatalogAssistantDictionarySuggestion>().WithMany().HasForeignKey(x => x.SuggestionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<CatalogRecognitionCandidate>().WithMany().HasForeignKey(x => x.CandidateId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.CreatedByUserId, x.SuggestionId, x.CreatedAtUtc });
    }
}
