using ElectronicService.Domain.Catalog.Manufacturers;
using ElectronicService.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

internal sealed class ManufacturerNoisePhraseConfiguration : IEntityTypeConfiguration<ManufacturerNoisePhrase>
{
    public void Configure(EntityTypeBuilder<ManufacturerNoisePhrase> builder)
    {
        builder.ToTable(
            "manufacturer_noise_phrases",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_manufacturer_noise_phrases_phrase_not_blank",
                    "char_length(btrim(\"phrase\")) > 0");

                table.HasCheckConstraint(
                    "ck_manufacturer_noise_phrases_normalized_phrase_not_blank",
                    "char_length(btrim(\"normalized_phrase\")) > 0");

                table.HasCheckConstraint(
                    "ck_manufacturer_noise_phrases_reason_not_blank",
                    "\"reason\" IS NULL OR char_length(btrim(\"reason\")) > 0");

                table.HasCheckConstraint(
                    "ck_manufacturer_noise_phrases_activity_dates",
                    "(\"is_active\" = TRUE AND \"deactivated_at_utc\" IS NULL) OR " +
                    "(\"is_active\" = FALSE AND \"deactivated_at_utc\" IS NOT NULL)");

                table.HasCheckConstraint(
                    "ck_manufacturer_noise_phrases_updated_after_created",
                    "\"updated_at_utc\" >= \"created_at_utc\"");

                table.HasCheckConstraint(
                    "ck_manufacturer_noise_phrases_deactivated_after_created",
                    "\"deactivated_at_utc\" IS NULL OR \"deactivated_at_utc\" >= \"created_at_utc\"");
            });

        builder.HasKey(noisePhrase => noisePhrase.Id);

        builder.Property(noisePhrase => noisePhrase.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(noisePhrase => noisePhrase.Phrase)
            .HasColumnName("phrase")
            .HasMaxLength(ManufacturerNoisePhrase.PhraseMaxLength)
            .IsRequired();

        builder.Property(noisePhrase => noisePhrase.NormalizedPhrase)
            .HasColumnName("normalized_phrase")
            .HasMaxLength(ManufacturerNoisePhrase.PhraseMaxLength)
            .IsRequired();

        builder.Property(noisePhrase => noisePhrase.Reason)
            .HasColumnName("reason")
            .HasMaxLength(ManufacturerNoisePhrase.ReasonMaxLength);

        builder.Property(noisePhrase => noisePhrase.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(noisePhrase => noisePhrase.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(noisePhrase => noisePhrase.UpdatedByUserId)
            .HasColumnName("updated_by_user_id")
            .IsRequired();

        builder.Property(noisePhrase => noisePhrase.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(noisePhrase => noisePhrase.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.Property(noisePhrase => noisePhrase.DeactivatedAtUtc)
            .HasColumnName("deactivated_at_utc");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(noisePhrase => noisePhrase.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(noisePhrase => noisePhrase.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(noisePhrase => noisePhrase.NormalizedPhrase)
            .IsUnique()
            .HasDatabaseName("ux_manufacturer_noise_phrases_normalized_phrase");

        builder.HasIndex(noisePhrase => noisePhrase.IsActive)
            .HasDatabaseName("ix_manufacturer_noise_phrases_is_active");

        builder.HasIndex(noisePhrase => noisePhrase.CreatedByUserId)
            .HasDatabaseName("ix_manufacturer_noise_phrases_created_by_user_id");

        builder.HasIndex(noisePhrase => noisePhrase.UpdatedByUserId)
            .HasDatabaseName("ix_manufacturer_noise_phrases_updated_by_user_id");
    }
}