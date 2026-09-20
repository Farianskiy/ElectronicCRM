using ElectronicService.Domain.Catalog.Recognition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

internal sealed class CatalogRecognitionMultiIntegerDraftPartConfiguration : IEntityTypeConfiguration<CatalogRecognitionMultiIntegerDraftPart>
{
    public void Configure(EntityTypeBuilder<CatalogRecognitionMultiIntegerDraftPart> builder)
    {
        builder.ToTable("catalog_recognition_multi_integer_draft_parts", table =>
        {
            table.HasCheckConstraint("ck_multi_integer_part_position", "\"position\" >= 0 AND \"position\" < 256");
            table.HasCheckConstraint(
                "ck_multi_integer_part_kind",
                "(\"characteristic_definition_id\" IS NULL AND \"literal\" IS NOT NULL AND char_length(\"literal\") > 0 AND \"distinct_value_count\" = 0) OR (\"characteristic_definition_id\" IS NOT NULL AND \"characteristic_definition_id\" <> '00000000-0000-0000-0000-000000000000'::uuid AND \"literal\" IS NULL AND \"distinct_value_count\" >= 2 AND \"distinct_value_count\" <= 200)");
        });

        builder.HasKey(part => new { part.DraftId, part.Position });
        builder.Property(part => part.DraftId).HasColumnName("draft_id").IsRequired();
        builder.Property(part => part.Position).HasColumnName("position").ValueGeneratedNever();
        builder.Property(part => part.Literal).HasColumnName("literal").HasMaxLength(2000).IsRequired(false);
        builder.Property(part => part.CharacteristicDefinitionId).HasColumnName("characteristic_definition_id").IsRequired(false);
        builder.Property(part => part.DistinctValueCount).HasColumnName("distinct_value_count").IsRequired();

        builder.HasIndex(part => new { part.DraftId, part.CharacteristicDefinitionId })
            .IsUnique()
            .HasFilter("\"characteristic_definition_id\" IS NOT NULL")
            .HasDatabaseName("ux_multi_integer_part_characteristic");

        builder.HasIndex(part => part.CharacteristicDefinitionId).HasDatabaseName("ix_multi_integer_part_characteristic");
    }
}