using ElectronicService.Domain.Catalog.Manufacturers;
using ElectronicService.Domain.Catalog.Products;
using ElectronicService.Domain.Catalog.ProductTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products", table =>
        {
            table.HasCheckConstraint(
                "ck_products_price_amount_not_negative",
                "\"price_amount\" >= 0");

            table.HasCheckConstraint(
                "ck_products_stock_quantity_not_negative",
                "\"stock_quantity\" >= 0");
        });

        builder.HasKey(product => product.Id);

        builder.Property(product => product.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.OwnsOne(product => product.Article, articleBuilder =>
        {
            articleBuilder.Property(article => article.Value)
                .HasColumnName("article")
                .HasMaxLength(100)
                .IsRequired();
        });

        builder.Navigation(product => product.Article)
            .IsRequired();

        builder.OwnsOne(product => product.Name, nameBuilder =>
        {
            nameBuilder.Property(name => name.Value)
                .HasColumnName("name")
                .HasMaxLength(500)
                .IsRequired();

            nameBuilder.Property(name => name.NormalizedValue)
                .HasColumnName("normalized_name")
                .HasMaxLength(500)
                .IsRequired();
        });

        builder.Navigation(product => product.Name)
            .IsRequired();

        builder.Property(product => product.ProductTypeId)
            .HasColumnName("product_type_id")
            .IsRequired();

        builder.Property(product => product.ManufacturerId)
            .HasColumnName("manufacturer_id")
            .IsRequired();

        builder.OwnsOne(product => product.Price, priceBuilder =>
        {
            priceBuilder.Property(price => price.Amount)
                .HasColumnName("price_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            priceBuilder.Property(price => price.Currency)
                .HasColumnName("price_currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Navigation(product => product.Price)
            .IsRequired();

        builder.OwnsOne(product => product.StockQuantity, stockBuilder =>
        {
            stockBuilder.Property(stock => stock.Value)
                .HasColumnName("stock_quantity")
                .HasPrecision(18, 3)
                .IsRequired();
        });

        builder.Navigation(product => product.StockQuantity)
            .IsRequired();

        builder.Property(product => product.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(product => product.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasOne<ProductType>()
            .WithMany()
            .HasForeignKey(product => product.ProductTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Manufacturer>()
            .WithMany()
            .HasForeignKey(product => product.ManufacturerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(product => product.Characteristics)
            .WithOne()
            .HasForeignKey(characteristic => characteristic.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(product => product.Characteristics)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(product => product.Aliases)
            .WithOne()
            .HasForeignKey(alias => alias.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(product => product.Aliases)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(product => product.ProductTypeId);

        builder.HasIndex(product => product.ManufacturerId);
    }
}