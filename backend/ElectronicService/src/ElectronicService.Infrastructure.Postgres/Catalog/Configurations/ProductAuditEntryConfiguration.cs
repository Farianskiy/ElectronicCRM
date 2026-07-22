using ElectronicService.Domain.Catalog.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres
    .Catalog.Configurations;

public sealed class ProductAuditEntryConfiguration
    : IEntityTypeConfiguration<ProductAuditEntry>
{
    public void Configure(
        EntityTypeBuilder<ProductAuditEntry> builder)
    {
        builder.ToTable(
            "product_audit_entries");

        builder.HasKey(auditEntry =>
            auditEntry.Id);

        builder.Property(auditEntry =>
                auditEntry.Id)
            .HasColumnName("id");

        builder.Property(auditEntry =>
                auditEntry.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(auditEntry =>
                auditEntry.ChangedByUserId)
            .HasColumnName("changed_by_user_id");

        builder.Property(auditEntry =>
                auditEntry.Operation)
            .HasColumnName("operation")
            .HasConversion<string>()
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(auditEntry =>
                auditEntry.Source)
            .HasColumnName("source")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(auditEntry =>
                auditEntry.SourceId)
            .HasColumnName("source_id");

        builder.Property(auditEntry =>
                auditEntry.ChangedAtUtc)
            .HasColumnName("changed_at_utc")
            .IsRequired();

        builder.Property(auditEntry =>
                auditEntry.BeforeJson)
            .HasColumnName("before_json")
            .HasColumnType("jsonb");

        builder.Property(auditEntry =>
                auditEntry.AfterJson)
            .HasColumnName("after_json")
            .HasColumnType("jsonb");

        builder.HasIndex(auditEntry =>
                auditEntry.ProductId)
            .HasDatabaseName(
                "ix_product_audit_entries_product_id");

        builder.HasIndex(auditEntry =>
                new
                {
                    auditEntry.ProductId,
                    auditEntry.ChangedAtUtc
                })
            .HasDatabaseName(
                "ix_product_audit_entries_product_date");

        builder.HasIndex(auditEntry =>
                auditEntry.ChangedByUserId)
            .HasDatabaseName(
                "ix_product_audit_entries_changed_by");

        builder.HasIndex(auditEntry =>
                auditEntry.SourceId)
            .HasDatabaseName(
                "ix_product_audit_entries_source_id");
    }
}