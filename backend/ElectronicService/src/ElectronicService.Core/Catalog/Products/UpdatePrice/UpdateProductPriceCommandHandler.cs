using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.Products.Audit;
using ElectronicService.Domain.Catalog.Audit;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Catalog.ValueObjects;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Products.UpdatePrice;

public sealed class UpdateProductPriceCommandHandler
{
    private readonly IProductRepository
        _productRepository;

    private readonly ProductAuditRecorder
        _auditRecorder;

    public UpdateProductPriceCommandHandler(
        IProductRepository productRepository,
        ProductAuditRecorder auditRecorder)
    {
        _productRepository = productRepository;
        _auditRecorder = auditRecorder;
    }

    public async Task<UnitResult<DomainError>> Handle(
        UpdateProductPriceCommand command,
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

        /*
         * Сначала проверяем новую цену.
         * Product пока не загружается и не изменяется.
         */
        var priceResult = Money.Create(
            command.Amount,
            command.Currency);

        if (priceResult.IsFailure)
        {
            return UnitResult.Failure(
                priceResult.Error);
        }

        /*
         * Для полного audit-снимка загружаем:
         *
         * Product
         * ProductCharacteristic
         * ProductAlias
         */
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

        /*
         * Состояние Product до изменения цены.
         */
        var beforeJson =
            ProductAuditSnapshotSerializer.Serialize(
                product);

        var changePriceResult =
            product.ChangePrice(
                priceResult.Value);

        if (changePriceResult.IsFailure)
        {
            return UnitResult.Failure(
                changePriceResult.Error);
        }

        /*
         * Product уже содержит новую цену.
         *
         * Recorder создаст afterJson и добавит
         * ProductAuditEntry в тот же DbContext.
         */
        var auditResult =
            _auditRecorder.RecordManualChange(
                product,
                command.ChangedByUserId,
                ProductAuditOperation.PriceUpdated,
                beforeJson);

        if (auditResult.IsFailure)
        {
            return UnitResult.Failure(
                auditResult.Error);
        }

        /*
         * Одновременно сохраняются:
         *
         * UPDATE products
         * INSERT product_audit_entries
         */
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