using ElectronicService.Domain.Catalog.Components;
using ElectronicService.Domain.Catalog.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

public sealed class ProductSelectedComponentConfiguration
    : IEntityTypeConfiguration<ProductSelectedComponent>
{
    public void Configure(EntityTypeBuilder<ProductSelectedComponent> builder)
    {
        builder.ToTable(
            "product_selected_components",
            table => table.HasCheckConstraint(
                "ck_product_selected_components_quantity",
                $"\"quantity\" >= 1 AND \"quantity\" <= {ProductSelectedComponent.MaximumQuantity}"));

        builder.HasKey(selection => selection.Id);

        builder.Property(selection => selection.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(selection => selection.MainProductId)
            .HasColumnName("main_product_id")
            .IsRequired();
        builder.Property(selection => selection.NeedDefinitionId)
            .HasColumnName("need_definition_id")
            .IsRequired();
        builder.Property(selection => selection.ComponentProductId)
            .HasColumnName("component_product_id")
            .IsRequired();
        builder.Property(selection => selection.Quantity)
            .HasColumnName("quantity")
            .IsRequired();
        builder.Property(selection => selection.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();
        builder.Property(selection => selection.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(selection => selection.MainProductId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(selection => selection.ComponentProductId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ComponentNeedDefinition>()
            .WithMany()
            .HasForeignKey(selection => selection.NeedDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(selection => new
        {
            selection.MainProductId,
            selection.NeedDefinitionId,
            selection.ComponentProductId
        }).IsUnique();
        builder.HasIndex(selection => selection.ComponentProductId);
        builder.HasIndex(selection => selection.NeedDefinitionId);
    }
}
