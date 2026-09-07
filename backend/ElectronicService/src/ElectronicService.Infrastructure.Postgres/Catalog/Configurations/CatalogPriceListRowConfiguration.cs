using ElectronicService.Domain.Catalog.PriceLists;
using ElectronicService.Domain.Catalog.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

public sealed class CatalogPriceListRowConfiguration
    : IEntityTypeConfiguration<CatalogPriceListRow>
{
    public void Configure(
        EntityTypeBuilder<CatalogPriceListRow> builder)
    {
        builder.ToTable(
            "catalog_price_list_rows",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_catalog_price_list_rows_number",
                    "\"row_number\" > 0");

                table.HasCheckConstraint(
                    "ck_catalog_price_list_rows_match_status",
                    "\"match_status\" <> 'None'");

                table.HasCheckConstraint(
                    "ck_catalog_price_list_rows_base_price",
                    "\"base_price_amount\" IS NULL "
                    + "OR \"base_price_amount\" >= 0 "
                    + "OR \"status\" = 'Error'");

                table.HasCheckConstraint(
                    "ck_catalog_price_list_rows_mrc_price",
                    "\"mrc_price_amount\" IS NULL "
                    + "OR \"mrc_price_amount\" >= 0 "
                    + "OR \"status\" = 'Error'");

                table.HasCheckConstraint(
                    "ck_catalog_price_list_rows_status",
                    "\"status\" <> 'None'");

                table.HasCheckConstraint(
                    "ck_catalog_price_list_rows_issues",
                    "(\"status\" = 'Error' "
                    + "AND jsonb_array_length(\"issues_json\") > 0) "
                    + "OR (\"status\" IN ('Pending', 'Valid') "
                    + "AND jsonb_array_length(\"issues_json\") = 0)");

                table.HasCheckConstraint(
                    "ck_catalog_price_list_rows_match_confidence",
                    "\"match_confidence_percent\" IS NULL "
                    + "OR (\"match_confidence_percent\" >= 0 "
                    + "AND \"match_confidence_percent\" <= 100)");

                table.HasCheckConstraint(
                    "ck_catalog_price_list_rows_match_product",
                    "(\"match_status\" IN "
                    + "('MatchedByArticle', 'MatchedByName', 'MatchedManually') "
                    + "AND \"product_id\" IS NOT NULL) "
                    + "OR (\"match_status\" IN "
                    + "('Pending', 'Ambiguous', 'ProductNotFound') "
                    + "AND \"product_id\" IS NULL)");
            });

        builder.HasKey(row =>
            row.Id);

        builder.Property(row =>
                row.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(row =>
                row.PriceListId)
            .HasColumnName("price_list_id")
            .IsRequired();

        builder.Property(row =>
                row.RowNumber)
            .HasColumnName("row_number")
            .IsRequired();

        builder.Property(row =>
                row.Article)
            .HasColumnName("article")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(row =>
                row.NormalizedArticle)
            .HasColumnName("normalized_article")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(row =>
                row.Name)
            .HasColumnName("name")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(row =>
                row.NormalizedName)
            .HasColumnName("normalized_name")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(row =>
                row.BasePriceAmount)
            .HasColumnName("base_price_amount")
            .HasPrecision(18, 4);

        builder.Property(row =>
                row.MrcPriceAmount)
            .HasColumnName("mrc_price_amount")
            .HasPrecision(18, 4);

        builder.Property(row =>
                row.ProductUrl)
            .HasColumnName("product_url")
            .HasConversion(
                productUrl =>
                    productUrl == null
                        ? null
                        : productUrl.OriginalString,
                storedValue =>
                    storedValue == null
                        ? null
                        : new Uri(
                            storedValue,
                            UriKind.Absolute))
            .HasMaxLength(
                CatalogPriceListRow.MaximumProductUrlLength);

        builder.Property(row =>
                row.Unit)
            .HasColumnName("unit")
            .HasColumnType("text");

        builder.Property(row =>
                row.ProductId)
            .HasColumnName("product_id");

        builder.Property(row =>
                row.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(row =>
                row.IssuesJson)
            .HasColumnName("issues_json")
            .HasColumnType("jsonb")
            .HasDefaultValue("[]")
            .IsRequired();

        builder.Property(row =>
                row.MatchStatus)
            .HasColumnName("match_status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(row =>
                row.MatchConfidencePercent)
            .HasColumnName("match_confidence_percent")
            .HasPrecision(5, 2);

        builder.HasOne<CatalogPriceList>()
            .WithMany()
            .HasForeignKey(row =>
                row.PriceListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(row =>
                row.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(row =>
                new
                {
                    row.PriceListId,
                    row.RowNumber
                })
            .IsUnique()
            .HasDatabaseName(
                "ux_catalog_price_list_rows_list_number");

        builder.HasIndex(row =>
                new
                {
                    row.PriceListId,
                    row.NormalizedArticle
                })
            .HasDatabaseName(
                "ix_catalog_price_list_rows_list_article");

        builder.HasIndex(row =>
                new
                {
                    row.PriceListId,
                    row.ProductId
                })
            .HasDatabaseName(
                "ix_catalog_price_list_rows_list_product");

        builder.HasIndex(row =>
                new
                {
                    row.PriceListId,
                    row.MatchStatus,
                    row.RowNumber
                })
            .HasDatabaseName(
                "ix_catalog_price_list_rows_list_match_status_number");

        builder.HasIndex(row =>
                new
                {
                    row.PriceListId,
                    row.Status,
                    row.RowNumber
                })
            .HasDatabaseName(
                "ix_catalog_price_list_rows_list_row_status_number");
    }
}