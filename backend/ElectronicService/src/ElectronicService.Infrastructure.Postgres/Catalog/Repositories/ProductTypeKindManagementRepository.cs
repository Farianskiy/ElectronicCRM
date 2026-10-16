using ElectronicService.Core.Catalog.ProductTypes.ChangeKind;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Catalog.PriceCalculations;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class ProductTypeKindManagementRepository
    : IProductTypeKindManagementRepository
{
    private readonly ElectronicDbContext _dbContext;

    public ProductTypeKindManagementRepository(ElectronicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ProductType?> GetByIdAsync(
        Guid productTypeId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ProductTypes
            .FirstOrDefaultAsync(
                item => item.Id == productTypeId,
                cancellationToken);
    }

    public async Task<ProductTypeKindChangeDependencySnapshot> GetDependenciesAsync(
        Guid productTypeId,
        CancellationToken cancellationToken = default)
    {
        var allProducts = _dbContext.Products
            .IgnoreQueryFilters()
            .Where(product => product.ProductTypeId == productTypeId);

        var productsCount = await _dbContext.Products
            .CountAsync(
                product => product.ProductTypeId == productTypeId,
                cancellationToken)
            .ConfigureAwait(false);

        var characteristicsCount = await _dbContext.ProductTypeCharacteristics
            .CountAsync(
                item => item.ProductTypeId == productTypeId,
                cancellationToken)
            .ConfigureAwait(false);

        var componentOffersCount = await (
            from offer in _dbContext.ComponentOffers
            join product in allProducts
                on offer.ComponentProductId equals product.Id
            select offer.Id)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var selectedComponentsCount = await (
            from selection in _dbContext.ProductSelectedComponents
            join product in allProducts
                on selection.ComponentProductId equals product.Id
            select selection.Id)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var draftCalculationComponentLinesCount = await (
            from component in _dbContext.CatalogPriceCalculationLineComponents
            join product in allProducts
                on component.ComponentProductId equals product.Id
            join line in _dbContext.CatalogPriceCalculationLines
                on component.CalculationLineId equals line.Id
            join calculation in _dbContext.CatalogPriceCalculations
                on line.CalculationId equals calculation.Id
            where calculation.Status == CatalogPriceCalculationStatus.Draft
            select component.Id)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var componentNeedsCount = await _dbContext.ComponentNeedDefinitions
            .CountAsync(
                need => need.MainProductTypeId == productTypeId,
                cancellationToken)
            .ConfigureAwait(false);

        var draftMainCalculationLinesCount = await (
            from line in _dbContext.CatalogPriceCalculationLines
            join product in allProducts
                on line.ProductId equals product.Id
            join calculation in _dbContext.CatalogPriceCalculations
                on line.CalculationId equals calculation.Id
            where calculation.Status == CatalogPriceCalculationStatus.Draft
            select line.Id)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var activeImportBatchesCount = await _dbContext.CatalogImportBatches
            .CountAsync(
                batch => batch.ProductTypeId == productTypeId
                         && batch.Status != CatalogImportBatchStatus.Applied
                         && batch.Status != CatalogImportBatchStatus.Rejected
                         && batch.Status != CatalogImportBatchStatus.Failed,
                cancellationToken)
            .ConfigureAwait(false);

        return new ProductTypeKindChangeDependencySnapshot(
            productsCount,
            characteristicsCount,
            componentOffersCount,
            selectedComponentsCount,
            draftCalculationComponentLinesCount,
            componentNeedsCount,
            draftMainCalculationLinesCount,
            activeImportBatchesCount);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
