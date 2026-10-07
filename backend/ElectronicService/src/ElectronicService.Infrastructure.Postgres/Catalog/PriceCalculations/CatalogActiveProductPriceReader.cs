using ElectronicService.Core.Catalog.PriceCalculations.Abstractions;
using ElectronicService.Domain.Catalog.PriceLists;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.PriceCalculations;

public sealed class CatalogActiveProductPriceReader
    : ICatalogActiveProductPriceReader
{
    private readonly ElectronicDbContext _dbContext;

    public CatalogActiveProductPriceReader(
        ElectronicDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public async Task<
        IReadOnlyList<CatalogActiveProductPriceSource>>
        FindByProductIdAsync(
            Guid productId,
            int take,
            CancellationToken cancellationToken = default)
    {
        var query =
            from row in
                _dbContext.CatalogPriceListRows
                    .AsNoTracking()
            join priceList in
                _dbContext.CatalogPriceLists
                    .AsNoTracking()
                on row.PriceListId
                equals priceList.Id
            join product in
                _dbContext.Products
                    .AsNoTracking()
                on row.ProductId
                equals product.Id
            join manufacturer in
                _dbContext.Manufacturers
                    .AsNoTracking()
                on priceList.ManufacturerId
                equals manufacturer.Id
            where row.ProductId == productId
                  && product.ManufacturerId
                      == priceList.ManufacturerId
                  && priceList.Status
                      == CatalogPriceListStatus.Active
                  && row.Status
                      == CatalogPriceListRowStatus.Valid
                  && row.BasePriceAmount.HasValue
            orderby row.RowNumber
            select new CatalogActiveProductPriceSource(
                product.Id,
                manufacturer.Id,
                manufacturer.Name,
                priceList.Id,
                row.Id,
                row.Article,
                row.Name,
                row.Unit,
                row.BasePriceAmount!.Value,
                row.MrcPriceAmount);

        var activeSources = await query
            .Take(take)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        if (activeSources.Length == 1)
        {
            return activeSources;
        }

        return await FindCatalogSourcesAsync([productId], cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<
        IReadOnlyList<CatalogActiveProductPriceSource>>
        FindByProductIdsAsync(
            IReadOnlyCollection<Guid> productIds,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(productIds);

        var distinctProductIds =
            productIds
                .Where(productId => productId != Guid.Empty)
                .Distinct()
                .ToArray();

        if (distinctProductIds.Length == 0)
        {
            return [];
        }

        var query =
            from row in
                _dbContext.CatalogPriceListRows
                    .AsNoTracking()
            join priceList in
                _dbContext.CatalogPriceLists
                    .AsNoTracking()
                on row.PriceListId
                equals priceList.Id
            join product in
                _dbContext.Products
                    .AsNoTracking()
                on row.ProductId
                equals product.Id
            join manufacturer in
                _dbContext.Manufacturers
                    .AsNoTracking()
                on priceList.ManufacturerId
                equals manufacturer.Id
            where row.ProductId.HasValue
                  && distinctProductIds.Contains(
                      row.ProductId.Value)
                  && product.ManufacturerId
                      == priceList.ManufacturerId
                  && priceList.Status
                      == CatalogPriceListStatus.Active
                  && row.Status
                      == CatalogPriceListRowStatus.Valid
                  && row.BasePriceAmount.HasValue
            orderby row.ProductId, row.RowNumber
            select new CatalogActiveProductPriceSource(
                product.Id,
                manufacturer.Id,
                manufacturer.Name,
                priceList.Id,
                row.Id,
                row.Article,
                row.Name,
                row.Unit,
                row.BasePriceAmount!.Value,
                row.MrcPriceAmount);

        var activeSources = await query
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var activeByProductId = activeSources
            .GroupBy(source => source.ProductId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var catalogFallbackIds = distinctProductIds
            .Where(productId =>
                !activeByProductId.TryGetValue(productId, out var sources)
                || sources.Length != 1)
            .ToArray();
        var catalogSources = await FindCatalogSourcesAsync(
                catalogFallbackIds,
                cancellationToken)
            .ConfigureAwait(false);
        var catalogByProductId = catalogSources.ToDictionary(
            source => source.ProductId);

        return distinctProductIds
            .Select(productId =>
            {
                if (activeByProductId.TryGetValue(productId, out var sources)
                    && sources.Length == 1)
                {
                    return sources[0];
                }

                return catalogByProductId.GetValueOrDefault(productId);
            })
            .Where(source => source is not null)
            .Cast<CatalogActiveProductPriceSource>()
            .ToArray();
    }

    private async Task<CatalogActiveProductPriceSource[]> FindCatalogSourcesAsync(
        Guid[] productIds,
        CancellationToken cancellationToken)
    {
        if (productIds.Length == 0)
        {
            return [];
        }

        return await (
                from product in _dbContext.Products.AsNoTracking()
                join manufacturer in _dbContext.Manufacturers.AsNoTracking()
                    on product.ManufacturerId equals manufacturer.Id
                where productIds.Contains(product.Id)
                select new CatalogActiveProductPriceSource(
                    product.Id,
                    manufacturer.Id,
                    manufacturer.Name,
                    null,
                    null,
                    product.Article.Value,
                    product.Name.Value,
                    null,
                    product.Price.Amount,
                    null))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
