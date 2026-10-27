using ElectronicService.Domain.Catalog.Products;
using ElectronicService.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

internal sealed class CatalogProductAnalogyEvaluationReportConfiguration
    : IEntityTypeConfiguration<CatalogProductAnalogyEvaluationReport>
{
    public void Configure(EntityTypeBuilder<CatalogProductAnalogyEvaluationReport> builder)
    {
        builder.ToTable("catalog_product_analogy_evaluation_reports");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ConfigurationId).HasColumnName("configuration_id").IsRequired();
        builder.Property(x => x.ConfigurationHash).HasColumnName("configuration_hash").HasMaxLength(64).IsRequired();
        builder.Property(x => x.BaselineConfigurationId).HasColumnName("baseline_configuration_id");
        builder.Property(x => x.BaselineConfigurationHash).HasColumnName("baseline_configuration_hash").HasMaxLength(64).IsRequired();
        builder.Property(x => x.CandidateSnapshotId).HasColumnName("candidate_snapshot_id").HasMaxLength(64).IsRequired();
        builder.Property(x => x.EvaluatorVersion).HasColumnName("evaluator_version").HasMaxLength(100).IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.ControlProducts).HasColumnName("control_products").IsRequired();
        builder.Property(x => x.ControlValues).HasColumnName("control_values").IsRequired();
        builder.Property(x => x.Improvements).HasColumnName("improvements").IsRequired();
        builder.Property(x => x.Regressions).HasColumnName("regressions").IsRequired();
        builder.Property(x => x.CandidateCorrect).HasColumnName("candidate_correct").IsRequired();
        builder.Property(x => x.CandidateIncorrect).HasColumnName("candidate_incorrect").IsRequired();
        builder.Property(x => x.IsReady).HasColumnName("is_ready").IsRequired();
        builder.Property(x => x.SnapshotJson).HasColumnName("snapshot_json").HasColumnType("text").IsRequired();
        builder.HasOne<CatalogProductAnalogyConfiguration>().WithMany().HasForeignKey(x => x.ConfigurationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<CatalogProductAnalogyConfiguration>().WithMany().HasForeignKey(x => x.BaselineConfigurationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ConfigurationId, x.CreatedAtUtc });
    }
}
