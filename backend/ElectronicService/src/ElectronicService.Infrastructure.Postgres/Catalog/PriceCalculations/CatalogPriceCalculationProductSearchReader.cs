using ElectronicService.Core.Catalog.PriceCalculations.Abstractions;
using ElectronicService.Domain.Catalog.PriceLists;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.PriceCalculations;

public sealed class CatalogPriceCalculationProductSearchReader
    : ICatalogPriceCalculationProductSearchReader
{
    private readonly ElectronicDbContext _dbContext;

    public CatalogPriceCalculationProductSearchReader(
        ElectronicDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public async Task<CatalogPriceCalculationProductsPage>
        SearchAsync(
            string? search,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
    {
        var eligibleRows =
            from row in
                _dbContext.CatalogPriceListRows
                    .AsNoTracking()
            join priceList in
                _dbContext.CatalogPriceLists
                    .AsNoTracking()
                on row.PriceListId
                equals priceList.Id
            where priceList.Status
                      == CatalogPriceListStatus.Active
                  && row.Status
                      == CatalogPriceListRowStatus.Valid
                  && row.ProductId.HasValue
                  && row.BasePriceAmount.HasValue
            select new
            {
                ProductId =
                    row.ProductId.GetValueOrDefault(),
                ManufacturerId =
                    priceList.ManufacturerId,
                PriceListId =
                    priceList.Id,
                PriceListRowId =
                    row.Id,
                PriceListEffectiveDate =
                    priceList.EffectiveDate,
                row.Article,
                row.NormalizedArticle,
                row.Name,
                row.NormalizedName,
                row.Unit,
                BasePriceAmount =
                    row.BasePriceAmount.GetValueOrDefault(),
                row.MrcPriceAmount
            };

        var uniquelyMatchedProductIds =
            eligibleRows
                .GroupBy(item =>
                    item.ProductId)
                .Where(group =>
                    group.Count() == 1)
                .Select(group =>
                    group.Key);

        var query =
            from priceSource in eligibleRows
            join product in
                _dbContext.Products.AsNoTracking()
                on priceSource.ProductId
                equals product.Id
            join manufacturer in
                _dbContext.Manufacturers.AsNoTracking()
                on priceSource.ManufacturerId
                equals manufacturer.Id
            where product.ManufacturerId
                      == priceSource.ManufacturerId
                  && uniquelyMatchedProductIds.Contains(
                      priceSource.ProductId)
            select new
            {
                priceSource.ProductId,
                priceSource.ManufacturerId,
                ManufacturerName =
                    manufacturer.Name,
                ManufacturerNormalizedName =
                    manufacturer.NormalizedName,
                priceSource.PriceListId,
                priceSource.PriceListRowId,
                priceSource.PriceListEffectiveDate,
                priceSource.Article,
                priceSource.NormalizedArticle,
                priceSource.Name,
                priceSource.NormalizedName,
                priceSource.Unit,
                priceSource.BasePriceAmount,
                priceSource.MrcPriceAmount,
                ProductArticle =
                    product.Article.Value,
                ProductNormalizedName =
                    product.Name.NormalizedValue
            };

        var normalizedSearch =
            NormalizeSearch(search);

        if (normalizedSearch is not null)
        {
            var escapedSearch =
                EscapeLikePattern(
                    normalizedSearch);

            var searchPattern =
                $"%{escapedSearch}%";

            query =
                query.Where(
                    item =>
                        EF.Functions.ILike(
                            item.NormalizedArticle,
                            searchPattern,
                            "\\")
                        || EF.Functions.ILike(
                            item.NormalizedName,
                            searchPattern,
                            "\\")
                        || EF.Functions.ILike(
                            item.ProductArticle,
                            searchPattern,
                            "\\")
                        || EF.Functions.ILike(
                            item.ProductNormalizedName,
                            searchPattern,
                            "\\")
                        || EF.Functions.ILike(
                            item.ManufacturerNormalizedName,
                            searchPattern,
                            "\\"));
        }

        var totalCount =
            await query
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

        var orderedQuery =
            normalizedSearch is null
                ? query
                    .OrderBy(item =>
                        item.ManufacturerName)
                    .ThenBy(item =>
                        item.Name)
                    .ThenBy(item =>
                        item.Article)
                : query
                    .OrderByDescending(
                        item =>
                            item.NormalizedArticle
                            == normalizedSearch)
                    .ThenByDescending(
                        item =>
                            item.ProductArticle
                            == normalizedSearch)
                    .ThenBy(item =>
                        item.ManufacturerName)
                    .ThenBy(item =>
                        item.Name)
                    .ThenBy(item =>
                        item.Article);

        var items =
            await orderedQuery
                .Skip(skip)
                .Take(take)
                .Select(
                    item =>
                        new CatalogPriceCalculationProductSearchItem(
                            item.ProductId,
                            item.ManufacturerId,
                            item.ManufacturerName,
                            item.PriceListId,
                            item.PriceListRowId,
                            item.PriceListEffectiveDate,
                            item.Article,
                            item.Name,
                            item.Unit,
                            item.BasePriceAmount,
                            item.MrcPriceAmount))
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

        return new CatalogPriceCalculationProductsPage(
            totalCount,
            items);
    }

    private static string? NormalizeSearch(
        string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return null;
        }

        return search
            .Trim()
            .ToUpperInvariant()
            .Replace(
                "Ё",
                "Е",
                StringComparison.Ordinal);
    }

    private static string EscapeLikePattern(
        string value)
    {
        return value
            .Replace(
                "\\",
                "\\\\",
                StringComparison.Ordinal)
            .Replace(
                "%",
                "\\%",
                StringComparison.Ordinal)
            .Replace(
                "_",
                "\\_",
                StringComparison.Ordinal);
    }
}