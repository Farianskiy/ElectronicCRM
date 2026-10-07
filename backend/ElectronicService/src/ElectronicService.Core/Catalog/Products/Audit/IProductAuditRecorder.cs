using CSharpFunctionalExtensions;
using ElectronicService.Domain.Catalog.Audit;
using ElectronicService.Domain.Catalog.Products;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Products.Audit;

public interface IProductAuditRecorder
{
    Task<Result<ProductAuditSnapshot, DomainError>> CaptureAsync(
        Product product,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyDictionary<Guid, ProductAuditSnapshot>, DomainError>>
        CaptureManyAsync(
            IReadOnlyCollection<Product> products,
            CancellationToken cancellationToken = default);

    Task<Result<ProductAuditRecordOutcome, DomainError>> RecordManualChangeAsync(
        Product product,
        Guid changedByUserId,
        ProductAuditOperation operation,
        ProductAuditSnapshot beforeSnapshot,
        CancellationToken cancellationToken = default);

    Result<ProductAuditRecordOutcome, DomainError> RecordPreparedManualChange(
        Product product,
        Guid changedByUserId,
        ProductAuditOperation operation,
        ProductAuditSnapshot beforeSnapshot,
        ProductAuditSnapshot afterSnapshot);
}
