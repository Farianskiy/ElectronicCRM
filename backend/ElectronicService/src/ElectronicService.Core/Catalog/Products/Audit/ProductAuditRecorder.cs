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

        var afterSnapshot =
            afterSnapshotResult.Value;

        /*
         * Не создаём audit entry и не вызываем
         * SaveChanges для операции, которая
         * ничего не изменила.
         */
        if (!ProductAuditSnapshotComparer
                .HasMeaningfulChanges(
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

        var afterJson =
            ProductAuditSnapshotSerializer
                .Serialize(afterSnapshot);

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