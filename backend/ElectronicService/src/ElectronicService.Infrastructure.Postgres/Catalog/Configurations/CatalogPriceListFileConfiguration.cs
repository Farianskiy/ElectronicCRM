using ElectronicService.Domain.Catalog.PriceLists;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

public sealed class CatalogPriceListFileConfiguration
    : IEntityTypeConfiguration<CatalogPriceListFile>
{
    public void Configure(
        EntityTypeBuilder<CatalogPriceListFile> builder)
    {
        builder.ToTable(
            "catalog_price_list_files",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_catalog_price_list_files_content",
                    "octet_length(\"content\") > 0 "
                    + $"AND octet_length(\"content\") <= {CatalogPriceList.MaximumFileSizeBytes}");
            });

        builder.HasKey(file =>
            file.PriceListId);

        builder.Property(file =>
                file.PriceListId)
            .HasColumnName("price_list_id")
            .ValueGeneratedNever();

        builder.Ignore(file =>
            file.Content);

        builder.Property<byte[]>(
                "ContentBytes")
            .HasColumnName("content")
            .HasColumnType("bytea")
            .IsRequired()
            .UsePropertyAccessMode(
                PropertyAccessMode.Property);
    }
}