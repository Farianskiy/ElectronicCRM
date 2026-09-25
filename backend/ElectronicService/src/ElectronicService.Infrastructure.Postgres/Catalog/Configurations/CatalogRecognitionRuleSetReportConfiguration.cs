using ElectronicService.Domain.Catalog.Recognition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

internal sealed class CatalogRecognitionRuleSetReportConfiguration
    : IEntityTypeConfiguration<CatalogRecognitionRuleSetReport>
{
    public void Configure(
        EntityTypeBuilder<CatalogRecognitionRuleSetReport> builder)
    {
        builder.ToTable("catalog_recognition_rule_set_reports", table =>
        {
            table.HasCheckConstraint(
                "ck_rule_set_report_counts",
                """
                "total_rows_count" > 0
                AND "proposed_rows_count" >= 0
                AND "conflict_rows_count" >= 0
                AND "no_match_rows_count" >= 0
                AND "outside_scope_rows_count" >= 0
                AND "total_rows_count"::bigint =
                    "proposed_rows_count"::bigint +
                    "conflict_rows_count"::bigint +
                    "no_match_rows_count"::bigint +
                    "outside_scope_rows_count"::bigint
                """);

            table.HasCheckConstraint(
                "ck_rule_set_report_time",
                "\"completed_at_utc\" >= \"started_at_utc\"");

            table.HasCheckConstraint(
                "ck_rule_set_report_batch_version",
                "\"batch_version\" BETWEEN 0 AND 4294967295");

            table.HasCheckConstraint(
                "ck_rule_set_report_format",
                "\"snapshot_format_version\" > 0");

            table.HasCheckConstraint(
                "ck_rule_set_report_snapshot",
                "jsonb_typeof(\"snapshot_json\") = 'array'");
        });

        builder.Property(report => report.EvaluationJson).HasColumnName("evaluation_json").HasColumnType("jsonb");
        builder.HasKey(report => report.Id);

        builder.Property(report => report.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(report => report.RuleSetVersionId)
            .HasColumnName("rule_set_version_id")
            .IsRequired();

        builder.Property(report => report.BatchId)
            .HasColumnName("batch_id")
            .IsRequired();

        builder.Property(report => report.BatchVersion)
            .HasColumnName("batch_version")
            .IsRequired();

        builder.Property(report => report.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(report => report.StartedAtUtc)
            .HasColumnName("started_at_utc")
            .IsRequired();

        builder.Property(report => report.CompletedAtUtc)
            .HasColumnName("completed_at_utc")
            .IsRequired();

        builder.Property(report => report.EvaluatorVersion)
            .HasColumnName("evaluator_version")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(report => report.SnapshotFormatVersion)
            .HasColumnName("snapshot_format_version")
            .IsRequired();

        builder.Property(report => report.TotalRowsCount)
            .HasColumnName("total_rows_count")
            .IsRequired();

        builder.Property(report => report.ProposedRowsCount)
            .HasColumnName("proposed_rows_count")
            .IsRequired();

        builder.Property(report => report.ConflictRowsCount)
            .HasColumnName("conflict_rows_count")
            .IsRequired();

        builder.Property(report => report.NoMatchRowsCount)
            .HasColumnName("no_match_rows_count")
            .IsRequired();

        builder.Property(report => report.OutsideScopeRowsCount)
            .HasColumnName("outside_scope_rows_count")
            .IsRequired();

        builder.Property(report => report.SnapshotJson)
            .HasColumnName("snapshot_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasOne<CatalogRecognitionRuleSetVersion>()
            .WithMany()
            .HasForeignKey(report => report.RuleSetVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(report => new
        {
            report.RuleSetVersionId,
            report.BatchId,
            report.CompletedAtUtc
        })
            .HasDatabaseName("ix_rule_set_report_version_batch_time");
    }
}