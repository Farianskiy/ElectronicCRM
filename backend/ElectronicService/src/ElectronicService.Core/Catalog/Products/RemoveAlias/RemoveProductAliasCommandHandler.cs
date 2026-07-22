using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.Products.Audit;
using ElectronicService.Domain.Catalog.Audit;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Products.RemoveAlias;

public sealed class RemoveProductAliasCommandHandler
{
    private readonly IProductRepository
        _productRepository;

    private readonly ProductAuditRecorder
        _auditRecorder;

    public RemoveProductAliasCommandHandler(
        IProductRepository productRepository,
        ProductAuditRecorder auditRecorder)
    {
        _productRepository = productRepository;
        _auditRecorder = auditRecorder;
    }

    public async Task<UnitResult<DomainError>> Handle(
        RemoveProductAliasCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ProductId == Guid.Empty)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(command.ProductId)));
        }

        if (command.ChangedByUserId == Guid.Empty)
        {
            return UnitResult.Failure(
                CatalogErrors.CurrentUserIsRequired());
        }

        if (command.AliasId == Guid.Empty)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(command.AliasId)));
        }

        var product = await _productRepository
            .GetByIdWithDetailsAsync(
                command.ProductId,
                cancellationToken)
            .ConfigureAwait(false);

        if (product is null)
        {
            return UnitResult.Failure(
                CatalogErrors.ProductNotFound(
                    command.ProductId.ToString()));
        }

        var beforeSnapshotResult =
            await _auditRecorder
                .CaptureAsync(
                    product,
                    cancellationToken)
                .ConfigureAwait(false);

        if (beforeSnapshotResult.IsFailure)
        {
            return UnitResult.Failure(
                beforeSnapshotResult.Error);
        }

        var beforeSnapshot =
            beforeSnapshotResult.Value;

        var removeResult =
            product.RemoveAlias(
                command.AliasId);

        if (removeResult.IsFailure)
        {
            return UnitResult.Failure(
                removeResult.Error);
        }

        var auditResult =
            await _auditRecorder
                .RecordManualChangeAsync(
                    product,
                    command.ChangedByUserId,
                    ProductAuditOperation.AliasRemoved,
                    beforeSnapshot,
                    cancellationToken)
                .ConfigureAwait(false);

        if (auditResult.IsFailure)
        {
            return UnitResult.Failure(
                auditResult.Error);
        }

        if (auditResult.Value
            == ProductAuditRecordOutcome.NoChanges)
        {
            /*
             * Доменный объект мог измениться в памяти,
             * например UpdatedAtUtc, но SaveChanges
             * не вызывается. База остаётся неизменной.
             */
            return UnitResult.Success<DomainError>();
        }

        var saved = await _productRepository
            .TrySaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!saved)
        {
            return UnitResult.Failure(
                CatalogErrors.ProductConcurrencyConflict(
                    command.ProductId));
        }

        return UnitResult.Success<DomainError>();
    }
}