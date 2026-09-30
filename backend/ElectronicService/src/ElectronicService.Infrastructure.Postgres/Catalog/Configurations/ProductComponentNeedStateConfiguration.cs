using ElectronicService.Domain.Catalog.Components;
using ElectronicService.Domain.Catalog.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

public sealed class ProductComponentNeedStateConfiguration
    : IEntityTypeConfiguration<ProductComponentNeedState>
{
    public void Configure(EntityTypeBuilder<ProductComponentNeedState> builder)
    {
        builder.ToTable(
            "product_component_need_states",
            table => table.HasCheckConstraint(
                "ck_product_component_need_status",
                "\"status\" IN ('Unknown', 'Missing', 'Included', 'NotApplicable')"));
        builder.HasKey(state => state.Id);
        builder.Property(state => state.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(state => state.ProductId)
            .HasColumnName("product_id")
            .IsRequired();
        builder.Property(state => state.NeedDefinitionId)
            .HasColumnName("need_definition_id")
            .IsRequired();
        builder.Property(state => state.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(state => state.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(state => state.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ComponentNeedDefinition>()
            .WithMany()
            .HasForeignKey(state => state.NeedDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(state => new { state.ProductId, state.NeedDefinitionId })
            .IsUnique();
    }
}
