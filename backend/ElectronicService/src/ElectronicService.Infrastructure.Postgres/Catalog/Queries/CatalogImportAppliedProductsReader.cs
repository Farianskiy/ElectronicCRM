using ElectronicService.Core.Catalog.ImportBatches.GetCatalogImportAppliedProducts;
using ElectronicService.Domain.Catalog.Audit;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Queries;

public sealed class CatalogImportAppliedProductsReader : ICatalogImportAppliedProductsReader
{
    private readonly ElectronicDbContext _dbContext;

    public CatalogImportAppliedProductsReader(ElectronicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CatalogImportAppliedProductsReadResult> ReadAsync(
        Guid batchId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query =
            from auditEntry in _dbContext.ProductAuditEntries.AsNoTracking()
            join product in _dbContext.Products.AsNoTracking()
                on auditEntry.ProductId equals product.Id
            join productType in _dbContext.ProductTypes.AsNoTracking()
                on product.ProductTypeId equals productType.Id
            join manufacturer in _dbContext.Manufacturers.AsNoTracking()
                on product.ManufacturerId equals manufacturer.Id
            where auditEntry.Source == ProductAuditSource.ImportBatch
                && auditEntry.Operation == ProductAuditOperation.ImportApplied
                && auditEntry.SourceId == batchId
            select new
            {
                AuditEntry = auditEntry,
                Product = product,
                ProductType = productType,
                Manufacturer = manufacturer
            };

        var totalCount = await query
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = await query
            .OrderBy(item => item.Product.Article.Value)
            .ThenBy(item => item.Product.Id)
            .Skip(skip)
            .Take(take)
            .Select(item => new CatalogImportAppliedProductItemResult(
                item.Product.Id,
                item.Product.Article.Value,
                item.Product.Name.Value,
                item.ProductType.Code,
                item.ProductType.Name,
                item.Manufacturer.Name,
                item.Product.Price.Amount,
                item.Product.Price.Currency,
                item.Product.StockQuantity.Value,
                item.AuditEntry.ChangedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new CatalogImportAppliedProductsReadResult(
            items,
            totalCount);
    }
}