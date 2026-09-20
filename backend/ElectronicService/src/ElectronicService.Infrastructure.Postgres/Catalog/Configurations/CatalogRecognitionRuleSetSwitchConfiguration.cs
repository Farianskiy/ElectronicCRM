using ElectronicService.Domain.Catalog.Recognition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

internal sealed class CatalogRecognitionRuleSetSwitchConfiguration
    : IEntityTypeConfiguration<CatalogRecognitionRuleSetSwitch>
{
    public void Configure(
        EntityTypeBuilder<CatalogRecognitionRuleSetSwitch> builder)
    {
        builder.ToTable("catalog_recognition_rule_set_switches", table =>
        {
            table.HasCheckConstraint(
                "ck_rule_set_switch_sequence",
                "\"sequence_number\" > 0");

            table.HasCheckConstraint(
                "ck_rule_set_switch_change",
                "\"previous_version_id\" IS DISTINCT FROM \"new_version_id\"");

            table.HasCheckConstraint(
                "ck_rule_set_switch_report",
                """
                ("new_version_id" IS NULL AND "report_id" IS NULL)
                OR
                ("new_version_id" IS NOT NULL AND "report_id" IS NOT NULL)
                """);

            table.HasCheckConstraint(
                "ck_rule_set_switch_first",
                "\"sequence_number\" <> 1 OR \"previous_version_id\" IS NULL");

            table.HasCheckConstraint(
                "ck_rule_set_switch_reason",
                "char_length(btrim(\"reason\")) > 0");
        });

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(item => item.ManufacturerId)
            .HasColumnName("manufacturer_id")
            .IsRequired();

        builder.Property(item => item.ProductTypeId)
            .HasColumnName("product_type_id")
            .IsRequired();

        builder.Property(item => item.SequenceNumber)
            .HasColumnName("sequence_number")
            .IsRequired();

        builder.Property(item => item.PreviousVersionId)
            .HasColumnName("previous_version_id");

        builder.Property(item => item.NewVersionId)
            .HasColumnName("new_version_id");

        builder.Property(item => item.ReportId)
            .HasColumnName("report_id");

        builder.Property(item => item.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(item => item.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(item => item.Reason)
            .HasColumnName("reason")
            .HasMaxLength(1000)
            .IsRequired();

        builder.HasOne<CatalogRecognitionRuleSetVersion>()
            .WithMany()
            .HasForeignKey(item => item.PreviousVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CatalogRecognitionRuleSetVersion>()
            .WithMany()
            .HasForeignKey(item => item.NewVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CatalogRecognitionRuleSetReport>()
            .WithMany()
            .HasForeignKey(item => item.ReportId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => new
        {
            item.ManufacturerId,
            item.ProductTypeId,
            item.SequenceNumber
        })
            .IsUnique()
            .HasDatabaseName("ux_rule_set_switch_scope_sequence");
    }
}