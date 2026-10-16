using ElectronicService.Domain.Catalog.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

public sealed class ComponentCompatibilityConditionGroupConfiguration
    : IEntityTypeConfiguration<ComponentCompatibilityConditionGroup>
{
    public void Configure(
        EntityTypeBuilder<ComponentCompatibilityConditionGroup> builder)
    {
        builder.ToTable("component_compatibility_condition_groups", table =>
        {
            table.HasCheckConstraint(
                "ck_component_condition_groups_position_non_negative",
                "\"position\" >= 0");
        });

        builder.HasKey(group => group.Id);
        builder.Property(group => group.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(group => group.ComponentOfferId)
            .HasColumnName("component_offer_id")
            .IsRequired();
        builder.Property(group => group.Position)
            .HasColumnName("position")
            .IsRequired();

        builder.HasMany(group => group.Constraints)
            .WithOne()
            .HasForeignKey(constraint => constraint.ConditionGroupId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(group => group.Constraints)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(group => new
        {
            group.ComponentOfferId,
            group.Position
        }).IsUnique();
    }
}
