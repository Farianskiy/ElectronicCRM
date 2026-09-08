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
                row.BasePriceAmount.GetValueOrDefault(),
                row.MrcPriceAmount);

        return await query
            .Take(take)
            .ToArrayAsync(cancellationToken)
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
                row.BasePriceAmount.GetValueOrDefault(),
                row.MrcPriceAmount);

        return await query
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
