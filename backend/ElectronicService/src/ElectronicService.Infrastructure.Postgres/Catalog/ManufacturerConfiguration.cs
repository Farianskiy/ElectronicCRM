using ElectronicService.Domain.Catalog.Manufacturers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog;

public sealed class ManufacturerConfiguration : IEntityTypeConfiguration<Manufacturer>
{
    public void Configure(EntityTypeBuilder<Manufacturer> builder)
    {
        builder.ToTable("manufacturers");

        builder.HasKey(manufacturer => manufacturer.Id);

        builder.Property(manufacturer => manufacturer.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(manufacturer => manufacturer.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(manufacturer => manufacturer.NormalizedName)
            .HasColumnName("normalized_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(manufacturer => manufacturer.NormalizedName)
            .IsUnique();
    }
}