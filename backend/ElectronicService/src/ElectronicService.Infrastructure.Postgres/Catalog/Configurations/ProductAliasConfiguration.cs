using ElectronicService.Domain.Catalog.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

public sealed class ProductAliasConfiguration : IEntityTypeConfiguration<ProductAlias>
{
    public void Configure(EntityTypeBuilder<ProductAlias> builder)
    {
        builder.ToTable("product_aliases");

        builder.HasKey(alias => alias.Id);

        builder.Property(alias => alias.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(alias => alias.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(alias => alias.Value)
            .HasColumnName("value")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(alias => alias.NormalizedValue)
            .HasColumnName("normalized_value")
            .HasMaxLength(500)
            .IsRequired();

        builder.HasOne<Product>()
            .WithMany(product => product.Aliases)
            .HasForeignKey(alias => alias.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(alias => alias.ProductId);

        builder.HasIndex(alias => new
        {
            alias.ProductId,
            alias.NormalizedValue
        })
            .IsUnique();

        builder.HasIndex(alias => alias.NormalizedValue);
    }
}