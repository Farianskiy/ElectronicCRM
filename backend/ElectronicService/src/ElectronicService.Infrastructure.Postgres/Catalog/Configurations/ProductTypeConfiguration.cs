using ElectronicService.Domain.Catalog.ProductTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

public sealed class ProductTypeConfiguration : IEntityTypeConfiguration<ProductType>
{
    public void Configure(EntityTypeBuilder<ProductType> builder)
    {
        builder.ToTable("product_types");

        builder.HasKey(productType => productType.Id);

        builder.Property(productType => productType.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(productType => productType.Code)
            .HasColumnName("code")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(productType => productType.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.HasMany(productType => productType.Characteristics)
            .WithOne()
            .HasForeignKey(productTypeCharacteristic => productTypeCharacteristic.ProductTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(productType => productType.Characteristics)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(productType => productType.Code)
            .IsUnique();

        builder.HasIndex(productType => productType.Name);
    }
}