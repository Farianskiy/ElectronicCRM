using ElectronicService.Core.Catalog.PriceCalculations.Abstractions;
using ElectronicService.Domain.Catalog.PriceLists;
using ElectronicService.Domain.Catalog.ProductTypes;
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
        return await SearchAsync(
                search,
                skip,
                take,
                productKind: null,
                productTypeCode: null,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<CatalogPriceCalculationProductsPage>
        SearchAsync(
            string? search,
            int skip,
            int take,
            ProductTypeKind? productKind,
            string? productTypeCode,
            CancellationToken cancellationToken = default)
    {
        var query =
            from product in
                _dbContext.Products.AsNoTracking()
            join manufacturer in
                _dbContext.Manufacturers.AsNoTracking()
                on product.ManufacturerId
                equals manufacturer.Id
            join productType in
                _dbContext.ProductTypes.AsNoTracking()
                on product.ProductTypeId
                equals productType.Id
            select new
            {
                ProductId = product.Id,
                ProductTypeCode = productType.Code,
                ProductTypeName = productType.Name,
                ProductTypeKind = productType.Kind,
                product.ManufacturerId,
                ManufacturerName =
                    manufacturer.Name,
                ManufacturerNormalizedName =
                    manufacturer.NormalizedName,
                Article = product.Article.Value,
                Name = product.Name.Value,
                CatalogPriceAmount = product.Price.Amount,
                NormalizedName = product.Name.NormalizedValue,
                Aliases = product.Aliases
            };

        if (productKind.HasValue)
        {
            query = query.Where(item => item.ProductTypeKind == productKind.Value);
        }

        if (!string.IsNullOrWhiteSpace(productTypeCode))
        {
            var normalizedProductTypeCode = productTypeCode.Trim().ToUpperInvariant();
            query = query.Where(item => item.ProductTypeCode == normalizedProductTypeCode);
        }

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
                            item.Article,
                            searchPattern,
                            "\\")
                        || EF.Functions.ILike(
                            item.NormalizedName,
                            searchPattern,
                            "\\")
                        || EF.Functions.ILike(
                            item.ManufacturerNormalizedName,
                            searchPattern,
                            "\\")
                        || item.Aliases.Any(
                            alias => EF.Functions.ILike(
                                alias.NormalizedValue,
                                searchPattern,
                                "\\")));
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
                            EF.Functions.ILike(
                                item.Article,
                                EscapeLikePattern(normalizedSearch),
                                "\\"))
                    .ThenBy(item =>
                        item.ManufacturerName)
                    .ThenBy(item =>
                        item.Name)
                    .ThenBy(item =>
                        item.Article);

        var products =
            await orderedQuery
                .Skip(skip)
                .Take(take)
                .Select(item => new
                {
                    item.ProductId,
                    item.ProductTypeCode,
                    item.ProductTypeName,
                    item.ManufacturerId,
                    item.ManufacturerName,
                    item.Article,
                    item.Name,
                    item.CatalogPriceAmount
                })
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

        var productIds = products
            .Select(product => product.ProductId)
            .ToArray();

        var priceSources = productIds.Length == 0
            ? []
            : await (
                from row in _dbContext.CatalogPriceListRows.AsNoTracking()
                join priceList in _dbContext.CatalogPriceLists.AsNoTracking()
                    on row.PriceListId equals priceList.Id
                where productIds.Contains(row.ProductId!.Value)
                      && row.ProductId.HasValue
                      && priceList.Status == CatalogPriceListStatus.Active
                      && row.Status == CatalogPriceListRowStatus.Valid
                      && row.BasePriceAmount.HasValue
                select new ProductPriceSource(
                    row.ProductId!.Value,
                    priceList.ManufacturerId,
                    priceList.Id,
                    row.Id,
                    priceList.EffectiveDate,
                    row.Article,
                    row.Name,
                    row.Unit,
                    row.BasePriceAmount!.Value,
                    row.MrcPriceAmount))
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

        var priceSourcesByProductId = priceSources
            .GroupBy(source => source.ProductId)
            .ToDictionary(group => group.Key, group => group.ToArray());

        var items = products
            .Select(product =>
            {
                var matchingPriceSources = priceSourcesByProductId
                    .GetValueOrDefault(product.ProductId, [])
                    .Where(source => source.ManufacturerId == product.ManufacturerId)
                    .ToArray();

                var priceSource = matchingPriceSources.Length == 1
                    ? matchingPriceSources[0]
                    : null;

                return new CatalogPriceCalculationProductSearchItem(
                    product.ProductId,
                    product.ProductTypeCode,
                    product.ProductTypeName,
                    product.ManufacturerId,
                    product.ManufacturerName,
                    CatalogPriceCalculationProductPriceStatus.Available,
                    priceSource?.PriceListId,
                    priceSource?.PriceListRowId,
                    priceSource?.PriceListEffectiveDate,
                    priceSource?.Article ?? product.Article,
                    priceSource?.Name ?? product.Name,
                    priceSource?.Unit,
                    priceSource?.BasePriceAmount ?? product.CatalogPriceAmount,
                    priceSource?.MrcPriceAmount);
            })
            .ToArray();

        return new CatalogPriceCalculationProductsPage(
            totalCount,
            items);
    }

    private sealed record ProductPriceSource(
        Guid ProductId,
        Guid ManufacturerId,
        Guid PriceListId,
        Guid PriceListRowId,
        DateOnly? PriceListEffectiveDate,
        string Article,
        string Name,
        string? Unit,
        decimal BasePriceAmount,
        decimal? MrcPriceAmount);

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
