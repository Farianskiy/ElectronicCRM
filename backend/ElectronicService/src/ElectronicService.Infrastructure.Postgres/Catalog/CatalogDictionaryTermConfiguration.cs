using ElectronicService.Domain.Catalog.Dictionaries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

internal sealed class CatalogDictionaryTermConfiguration
    : IEntityTypeConfiguration<CatalogDictionaryTerm>
{
    public void Configure(EntityTypeBuilder<CatalogDictionaryTerm> builder)
    {
        builder.ToTable("catalog_dictionary_terms");

        builder.HasKey(term => term.Id);

        builder.Property(term => term.Id)
            .HasColumnName("id");

        builder.Property(term => term.Phrase)
            .HasColumnName("phrase")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(term => term.NormalizedPhrase)
            .HasColumnName("normalized_phrase")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(term => term.Kind)
            .HasColumnName("kind")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(term => term.TargetCode)
            .HasColumnName("target_code")
            .HasMaxLength(100);

        builder.Property(term => term.TargetValue)
            .HasColumnName("target_value")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(term => term.Priority)
            .HasColumnName("priority")
            .IsRequired();

        builder.Property(term => term.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(term => term.Source)
            .HasColumnName("source")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(term => term.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(term => term.ApprovedAtUtc)
            .HasColumnName("approved_at_utc");

        builder.HasIndex(term => term.NormalizedPhrase)
            .HasDatabaseName("ix_catalog_dictionary_terms_normalized_phrase");

        builder.HasIndex(term => term.Status)
            .HasDatabaseName("ix_catalog_dictionary_terms_status");

        builder.HasIndex(term => new
            {
                term.NormalizedPhrase,
                term.Kind,
                term.TargetCode,
                term.TargetValue
            })
            .HasDatabaseName("ix_catalog_dictionary_terms_mapping");
    }
}