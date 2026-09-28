using ElectronicService.Domain.Catalog.Components;
using ElectronicService.Domain.Catalog.ProductTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

public sealed class ComponentNeedDefinitionConfiguration
    : IEntityTypeConfiguration<ComponentNeedDefinition>
{
    public void Configure(EntityTypeBuilder<ComponentNeedDefinition> builder)
    {
        builder.ToTable("component_need_definitions");
        builder.HasKey(need => need.Id);

        builder.Property(need => need.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(need => need.MainProductTypeId)
            .HasColumnName("main_product_type_id")
            .IsRequired();
        builder.Property(need => need.Code)
            .HasColumnName("code")
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(need => need.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(need => need.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasOne<ProductType>()
            .WithMany()
            .HasForeignKey(need => need.MainProductTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(need => new { need.MainProductTypeId, need.Code })
            .IsUnique();
    }
}
