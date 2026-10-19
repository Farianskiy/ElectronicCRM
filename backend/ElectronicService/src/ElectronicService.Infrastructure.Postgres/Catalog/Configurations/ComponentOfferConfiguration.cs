using ElectronicService.Domain.Catalog.Components;
using ElectronicService.Domain.Catalog.Products;
using ElectronicService.Domain.Catalog.ProductTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

public sealed class ComponentOfferConfiguration
    : IEntityTypeConfiguration<ComponentOffer>
{
    public void Configure(EntityTypeBuilder<ComponentOffer> builder)
    {
        builder.ToTable("component_offers");
        builder.HasKey(offer => offer.Id);

        builder.Property(offer => offer.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(offer => offer.ComponentProductId)
            .HasColumnName("component_product_id")
            .IsRequired();
        builder.Property(offer => offer.MainProductTypeId)
            .HasColumnName("main_product_type_id")
            .IsRequired();
        builder.Property(offer => offer.NeedDefinitionId)
            .HasColumnName("need_definition_id");
        builder.Property(offer => offer.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();
        builder.Property(offer => offer.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");
        builder.Property(offer => offer.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(offer => offer.ComponentProductId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ComponentNeedDefinition>()
            .WithMany()
            .HasForeignKey(offer => offer.NeedDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ProductType>()
            .WithMany()
            .HasForeignKey(offer => offer.MainProductTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(offer => offer.ConditionGroups)
            .WithOne()
            .HasForeignKey(group => group.ComponentOfferId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(offer => offer.ConditionGroups)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(offer => new
        {
            offer.ComponentProductId,
            offer.NeedDefinitionId
        })
            .IsUnique()
            .HasFilter("\"need_definition_id\" IS NOT NULL");
        builder.HasIndex(offer => new
        {
            offer.MainProductTypeId,
            offer.ComponentProductId
        })
            .IsUnique()
            .HasFilter("\"need_definition_id\" IS NULL");
    }
}
