using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Products.Audit;
using ElectronicService.Domain.Catalog.Audit;
using ElectronicService.Domain.Catalog.Products;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.UnitTests.TestDoubles;

internal sealed class FakeProductAuditRecorder : IProductAuditRecorder
{
    public Task<Result<ProductAuditSnapshot, DomainError>> CaptureAsync(
        Product product,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success<ProductAuditSnapshot, DomainError>(
            CreateSnapshot(product)));
    }

    public Task<Result<IReadOnlyDictionary<Guid, ProductAuditSnapshot>, DomainError>>
        CaptureManyAsync(
            IReadOnlyCollection<Product> products,
            CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<Guid, ProductAuditSnapshot> snapshots = products
            .ToDictionary(product => product.Id, CreateSnapshot);

        return Task.FromResult(Result.Success<
            IReadOnlyDictionary<Guid, ProductAuditSnapshot>,
            DomainError>(snapshots));
    }

    public Task<Result<ProductAuditRecordOutcome, DomainError>>
        RecordManualChangeAsync(
            Product product,
            Guid changedByUserId,
            ProductAuditOperation operation,
            ProductAuditSnapshot beforeSnapshot,
            CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success<
            ProductAuditRecordOutcome,
            DomainError>(ProductAuditRecordOutcome.Recorded));
    }

    public Result<ProductAuditRecordOutcome, DomainError>
        RecordPreparedManualChange(
            Product product,
            Guid changedByUserId,
            ProductAuditOperation operation,
            ProductAuditSnapshot beforeSnapshot,
            ProductAuditSnapshot afterSnapshot)
    {
        return ProductAuditRecordOutcome.Recorded;
    }

    private static ProductAuditSnapshot CreateSnapshot(Product product)
    {
        return new ProductAuditSnapshot(
            ProductAuditSnapshotVersions.Current,
            product.Id,
            product.Article.Value,
            product.Name.Value,
            product.ProductTypeId,
            null,
            null,
            product.ManufacturerId,
            null,
            product.Price.Amount,
            product.Price.Currency,
            product.StockQuantity.Value,
            [],
            []);
    }
}
