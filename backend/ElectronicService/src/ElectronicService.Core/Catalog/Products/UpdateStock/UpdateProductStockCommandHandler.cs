using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.Products.Audit;
using ElectronicService.Domain.Catalog.Audit;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Catalog.ValueObjects;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Products.UpdateStock;

public sealed class UpdateProductStockCommandHandler
{
    private readonly IProductRepository
        _productRepository;

    private readonly ProductAuditRecorder
        _auditRecorder;

    public UpdateProductStockCommandHandler(
        IProductRepository productRepository,
        ProductAuditRecorder auditRecorder)
    {
        _productRepository = productRepository;
        _auditRecorder = auditRecorder;
    }

    public async Task<UnitResult<DomainError>> Handle(
        UpdateProductStockCommand command,
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

        var stockQuantityResult =
            StockQuantity.Create(
                command.Quantity);

        if (stockQuantityResult.IsFailure)
        {
            return UnitResult.Failure(
                stockQuantityResult.Error);
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

        var changeStockQuantityResult =
            product.ChangeStockQuantity(
                stockQuantityResult.Value);

        if (changeStockQuantityResult.IsFailure)
        {
            return UnitResult.Failure(
                changeStockQuantityResult.Error);
        }

        var auditResult =
            await _auditRecorder
                .RecordManualChangeAsync(
                    product,
                    command.ChangedByUserId,
                    ProductAuditOperation.StockUpdated,
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