using CSharpFunctionalExtensions;
using ElectronicService.Domain.Catalog.Audit;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Catalog.Products;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Products.Audit;

public sealed class ProductAuditRecorder
{
    private readonly IProductAuditRepository
        _auditRepository;

    private readonly ProductAuditSnapshotBuilder
        _snapshotBuilder;

    public ProductAuditRecorder(
        IProductAuditRepository auditRepository,
        ProductAuditSnapshotBuilder snapshotBuilder)
    {
        _auditRepository = auditRepository;
        _snapshotBuilder = snapshotBuilder;
    }

    public Task<Result<
        ProductAuditSnapshot,
        DomainError>> CaptureAsync(
            Product product,
            CancellationToken cancellationToken =
                default)
    {
        return _snapshotBuilder.BuildAsync(
            product,
            cancellationToken);
    }

    public Task<Result<
        IReadOnlyDictionary<Guid, ProductAuditSnapshot>,
        DomainError>> CaptureManyAsync(
            IReadOnlyCollection<Product> products,
            CancellationToken cancellationToken = default)
    {
        return _snapshotBuilder.BuildManyAsync(products, cancellationToken);
    }

    public async Task<Result<
        ProductAuditRecordOutcome,
        DomainError>> RecordManualChangeAsync(
            Product product,
            Guid changedByUserId,
            ProductAuditOperation operation,
            ProductAuditSnapshot beforeSnapshot,
            CancellationToken cancellationToken =
                default)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(
            beforeSnapshot);

        if (changedByUserId == Guid.Empty)
        {
            return Result.Failure<
                ProductAuditRecordOutcome,
                DomainError>(
                    CatalogErrors
                        .CurrentUserIsRequired());
        }

        if (beforeSnapshot.ProductId != product.Id)
        {
            return Result.Failure<
                ProductAuditRecordOutcome,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(beforeSnapshot)));
        }

        var afterSnapshotResult =
            await _snapshotBuilder
                .BuildAsync(
                    product,
                    cancellationToken)
                .ConfigureAwait(false);

        if (afterSnapshotResult.IsFailure)
        {
            return Result.Failure<
                ProductAuditRecordOutcome,
                DomainError>(
                    afterSnapshotResult.Error);
        }

        return RecordPreparedManualChange(
            product,
            changedByUserId,
            operation,
            beforeSnapshot,
            afterSnapshotResult.Value);
    }

    public Result<ProductAuditRecordOutcome, DomainError> RecordPreparedManualChange(
        Product product,
        Guid changedByUserId,
        ProductAuditOperation operation,
        ProductAuditSnapshot beforeSnapshot,
        ProductAuditSnapshot afterSnapshot)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(beforeSnapshot);
        ArgumentNullException.ThrowIfNull(afterSnapshot);

        if (changedByUserId == Guid.Empty)
        {
            return CatalogErrors.CurrentUserIsRequired();
        }

        if (beforeSnapshot.ProductId != product.Id
            || afterSnapshot.ProductId != product.Id)
        {
            return GeneralErrors.ValueIsInvalid(nameof(beforeSnapshot));
        }

        if (!ProductAuditSnapshotComparer.HasMeaningfulChanges(
                beforeSnapshot,
                afterSnapshot))
        {
            return Result.Success<
                ProductAuditRecordOutcome,
                DomainError>(
                    ProductAuditRecordOutcome
                        .NoChanges);
        }

        var beforeJson =
            ProductAuditSnapshotSerializer
                .Serialize(beforeSnapshot);

        var afterJson = ProductAuditSnapshotSerializer.Serialize(afterSnapshot);

        var auditEntryResult =
            ProductAuditEntry.Create(
                product.Id,
                changedByUserId,
                operation,
                ProductAuditSource.Manual,
                sourceId: null,
                beforeJson,
                afterJson);

        if (auditEntryResult.IsFailure)
        {
            return Result.Failure<
                ProductAuditRecordOutcome,
                DomainError>(
                    auditEntryResult.Error);
        }

        _auditRepository.Add(
            auditEntryResult.Value);

        return Result.Success<
            ProductAuditRecordOutcome,
            DomainError>(
                ProductAuditRecordOutcome.Recorded);
    }
}
