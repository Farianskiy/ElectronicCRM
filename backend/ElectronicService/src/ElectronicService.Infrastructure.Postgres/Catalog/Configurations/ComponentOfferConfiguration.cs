using ElectronicService.Domain.Catalog.Components;
using ElectronicService.Domain.Catalog.Products;
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
        builder.Property(offer => offer.NeedDefinitionId)
            .HasColumnName("need_definition_id")
            .IsRequired();
        builder.Property(offer => offer.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(offer => offer.ComponentProductId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ComponentNeedDefinition>()
            .WithMany()
            .HasForeignKey(offer => offer.NeedDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(offer => offer.Constraints)
            .WithOne()
            .HasForeignKey(constraint => constraint.ComponentOfferId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(offer => offer.Constraints)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(offer => new
        {
            offer.ComponentProductId,
            offer.NeedDefinitionId
        }).IsUnique();
    }
}
