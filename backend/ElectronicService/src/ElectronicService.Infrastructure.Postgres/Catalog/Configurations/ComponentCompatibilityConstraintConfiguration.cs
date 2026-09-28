using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

public sealed class ComponentCompatibilityConstraintConfiguration
    : IEntityTypeConfiguration<ComponentCompatibilityConstraint>
{
    public void Configure(
        EntityTypeBuilder<ComponentCompatibilityConstraint> builder)
    {
        builder.ToTable("component_compatibility_constraints", table =>
        {
            table.HasCheckConstraint(
                "ck_component_constraints_data_type_not_none",
                "\"expected_data_type\" <> 'None'");
            table.HasCheckConstraint(
                "ck_component_constraints_only_one_value_type",
                """
                (
                    "expected_data_type" = 'Text'
                    AND "expected_text" IS NOT NULL
                    AND "expected_number" IS NULL
                    AND "expected_boolean" IS NULL
                )
                OR
                (
                    "expected_data_type" = 'Number'
                    AND "expected_text" IS NULL
                    AND "expected_number" IS NOT NULL
                    AND "expected_boolean" IS NULL
                )
                OR
                (
                    "expected_data_type" = 'Boolean'
                    AND "expected_text" IS NULL
                    AND "expected_number" IS NULL
                    AND "expected_boolean" IS NOT NULL
                )
                """);
        });

        builder.HasKey(constraint => constraint.Id);
        builder.Property(constraint => constraint.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(constraint => constraint.ComponentOfferId)
            .HasColumnName("component_offer_id")
            .IsRequired();
        builder.Property(constraint => constraint.CharacteristicDefinitionId)
            .HasColumnName("characteristic_definition_id")
            .IsRequired();

        builder.OwnsOne(constraint => constraint.ExpectedValue, valueBuilder =>
        {
            valueBuilder.Property(value => value.DataType)
                .HasColumnName("expected_data_type")
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            valueBuilder.Property(value => value.TextValue)
                .HasColumnName("expected_text")
                .HasMaxLength(1000);
            valueBuilder.Property(value => value.NumberValue)
                .HasColumnName("expected_number")
                .HasPrecision(18, 4);
            valueBuilder.Property(value => value.BooleanValue)
                .HasColumnName("expected_boolean");
        });
        builder.Navigation(constraint => constraint.ExpectedValue).IsRequired();

        builder.HasOne<CharacteristicDefinition>()
            .WithMany()
            .HasForeignKey(constraint => constraint.CharacteristicDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(constraint => constraint.ComponentOfferId);
        builder.HasIndex(constraint => constraint.CharacteristicDefinitionId);
    }
}
