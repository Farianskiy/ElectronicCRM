using ElectronicService.Domain.Catalog.Manufacturers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

internal sealed class ManufacturerAliasConfiguration : IEntityTypeConfiguration<ManufacturerAlias>
{
    public void Configure(EntityTypeBuilder<ManufacturerAlias> builder)
    {
        builder.ToTable(
            "manufacturer_aliases",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_manufacturer_aliases_status",
                    "\"status\" IN ('Pending', 'Approved', 'Rejected')");

                table.HasCheckConstraint(
                    "ck_manufacturer_aliases_source",
                    "\"source\" IN ('Seed', 'Import', 'TechnicalUser', 'UserCorrection', 'RecognitionLearning')");

                table.HasCheckConstraint(
                    "ck_manufacturer_aliases_status_dates",
                    "(" +
                    "\"status\" = 'Pending' " +
                    "AND \"approved_at_utc\" IS NULL " +
                    "AND \"rejected_at_utc\" IS NULL" +
                    ") OR (" +
                    "\"status\" = 'Approved' " +
                    "AND \"approved_at_utc\" IS NOT NULL " +
                    "AND \"rejected_at_utc\" IS NULL" +
                    ") OR (" +
                    "\"status\" = 'Rejected' " +
                    "AND \"approved_at_utc\" IS NULL " +
                    "AND \"rejected_at_utc\" IS NOT NULL" +
                    ")");

                table.HasCheckConstraint(
                    "ck_manufacturer_aliases_updated_after_created",
                    "\"updated_at_utc\" >= \"created_at_utc\"");

                table.HasCheckConstraint(
                    "ck_manufacturer_aliases_approved_after_created",
                    "\"approved_at_utc\" IS NULL OR \"approved_at_utc\" >= \"created_at_utc\"");

                table.HasCheckConstraint(
                    "ck_manufacturer_aliases_rejected_after_created",
                    "\"rejected_at_utc\" IS NULL OR \"rejected_at_utc\" >= \"created_at_utc\"");
            });

        builder.HasKey(manufacturerAlias => manufacturerAlias.Id);

        builder.Property(manufacturerAlias => manufacturerAlias.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(manufacturerAlias => manufacturerAlias.ManufacturerId)
            .HasColumnName("manufacturer_id")
            .IsRequired();

        builder.Property(manufacturerAlias => manufacturerAlias.Phrase)
            .HasColumnName("phrase")
            .HasMaxLength(ManufacturerAlias.PhraseMaxLength)
            .IsRequired();

        builder.Property(manufacturerAlias => manufacturerAlias.NormalizedPhrase)
            .HasColumnName("normalized_phrase")
            .HasMaxLength(ManufacturerAlias.PhraseMaxLength)
            .IsRequired();

        builder.Property(manufacturerAlias => manufacturerAlias.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(manufacturerAlias => manufacturerAlias.Source)
            .HasColumnName("source")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(manufacturerAlias => manufacturerAlias.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(manufacturerAlias => manufacturerAlias.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.Property(manufacturerAlias => manufacturerAlias.ApprovedAtUtc)
            .HasColumnName("approved_at_utc");

        builder.Property(manufacturerAlias => manufacturerAlias.RejectedAtUtc)
            .HasColumnName("rejected_at_utc");

        builder.HasOne<Manufacturer>()
            .WithMany()
            .HasForeignKey(manufacturerAlias => manufacturerAlias.ManufacturerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(manufacturerAlias => manufacturerAlias.NormalizedPhrase)
            .IsUnique()
            .HasFilter("\"status\" IN ('Pending', 'Approved')")
            .HasDatabaseName("ux_manufacturer_aliases_active_normalized_phrase");

        builder.HasIndex(manufacturerAlias => manufacturerAlias.Status)
            .HasDatabaseName("ix_manufacturer_aliases_status");

        builder.HasIndex(manufacturerAlias => new
        {
            manufacturerAlias.ManufacturerId,
            manufacturerAlias.Status
        })
            .HasDatabaseName("ix_manufacturer_aliases_manufacturer_status");
    }
}