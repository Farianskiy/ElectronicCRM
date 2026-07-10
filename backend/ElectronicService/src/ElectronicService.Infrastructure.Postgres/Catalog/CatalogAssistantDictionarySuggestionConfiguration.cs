using ElectronicService.Domain.Catalog.Dictionaries;
using ElectronicService.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

internal sealed class CatalogAssistantDictionarySuggestionConfiguration
    : IEntityTypeConfiguration<CatalogAssistantDictionarySuggestion>
{
    public void Configure(EntityTypeBuilder<CatalogAssistantDictionarySuggestion> builder)
    {
        builder.ToTable("catalog_assistant_dictionary_suggestions");

        builder.HasKey(suggestion => suggestion.Id);

        builder.Property(suggestion => suggestion.Id)
            .HasColumnName("id");

        builder.Property(suggestion => suggestion.OriginalMessage)
            .HasColumnName("original_message")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(suggestion => suggestion.UnknownPhrase)
            .HasColumnName("unknown_phrase")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(suggestion => suggestion.NormalizedUnknownPhrase)
            .HasColumnName("normalized_unknown_phrase")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(suggestion => suggestion.SuggestedKind)
            .HasColumnName("suggested_kind")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(suggestion => suggestion.SuggestedTargetCode)
            .HasColumnName("suggested_target_code")
            .HasMaxLength(100);

        builder.Property(suggestion => suggestion.SuggestedTargetValue)
            .HasColumnName("suggested_target_value")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(suggestion => suggestion.Confidence)
            .HasColumnName("confidence")
            .HasPrecision(5, 4)
            .IsRequired();

        builder.Property(suggestion => suggestion.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(suggestion => suggestion.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(suggestion => suggestion.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(suggestion => suggestion.ReviewedByUserId)
            .HasColumnName("reviewed_by_user_id");

        builder.Property(suggestion => suggestion.ReviewedAtUtc)
            .HasColumnName("reviewed_at_utc");

        builder.Property(suggestion => suggestion.ReviewComment)
            .HasColumnName("review_comment")
            .HasMaxLength(1000);

        builder.HasIndex(suggestion => suggestion.Status)
            .HasDatabaseName("ix_catalog_assistant_dictionary_suggestions_status");

        builder.HasIndex(suggestion => suggestion.NormalizedUnknownPhrase)
            .HasDatabaseName("ix_catalog_assistant_dictionary_suggestions_normalized_unknown_phrase");

        builder.HasIndex(suggestion => suggestion.CreatedAtUtc)
            .HasDatabaseName("ix_catalog_assistant_dictionary_suggestions_created_at_utc");

        builder.HasIndex(suggestion => suggestion.CreatedByUserId)
            .HasDatabaseName("ix_catalog_assistant_dictionary_suggestions_created_by_user_id");

        builder.HasIndex(suggestion => suggestion.ReviewedByUserId)
            .HasDatabaseName("ix_catalog_assistant_dictionary_suggestions_reviewed_by_user_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(suggestion => suggestion.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(suggestion => suggestion.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}