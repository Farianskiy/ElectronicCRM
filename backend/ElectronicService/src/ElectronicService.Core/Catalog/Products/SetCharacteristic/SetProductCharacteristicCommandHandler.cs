using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.Products.Audit;
using ElectronicService.Domain.Catalog.Audit;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Catalog.ValueObjects;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Products.SetCharacteristic;

public sealed class SetProductCharacteristicCommandHandler
{
    private readonly IProductRepository
        _productRepository;

    private readonly ICatalogProductMetadataRepository
        _metadataRepository;

    private readonly IProductAuditRecorder
        _auditRecorder;

    public SetProductCharacteristicCommandHandler(
        IProductRepository productRepository,
        ICatalogProductMetadataRepository metadataRepository,
        IProductAuditRecorder auditRecorder)
    {
        _productRepository = productRepository;
        _metadataRepository = metadataRepository;
        _auditRecorder = auditRecorder;
    }

    public async Task<UnitResult<DomainError>> Handle(
        SetProductCharacteristicCommand command,
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

        if (string.IsNullOrWhiteSpace(command.Code))
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(command.Code)));
        }

        if (string.IsNullOrWhiteSpace(command.Value))
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(command.Value)));
        }

        /*
         * Для полного audit-снимка нужны:
         *
         * основные данные
         * характеристики
         * aliases.
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

        var productType = await _metadataRepository
            .GetProductTypeByIdAsync(
                product.ProductTypeId,
                cancellationToken)
            .ConfigureAwait(false);

        if (productType is null)
        {
            return UnitResult.Failure(
                CatalogErrors.ProductTypeNotFound(
                    product.ProductTypeId.ToString()));
        }

        var definition = await _metadataRepository
            .GetCharacteristicDefinitionByCodeAsync(
                command.Code,
                cancellationToken)
            .ConfigureAwait(false);

        if (definition is null)
        {
            return UnitResult.Failure(
                CatalogErrors
                    .CharacteristicDefinitionNotFound(
                        command.Code));
        }

        /*
         * Сначала преобразуем строковое значение
         * в нужный тип.
         *
         * До успешного результата Product
         * не изменяется.
         */
        var characteristicValueResult =
            ProductCharacteristicValueFactory.Create(
                definition.Code,
                definition.DataType,
                command.Value);

        if (characteristicValueResult.IsFailure)
        {
            return UnitResult.Failure(
                characteristicValueResult.Error);
        }

        /*
         * Все внешние данные найдены,
         * а значение прошло предварительную
         * валидацию — снимаем состояние "до".
         */
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

        var setCharacteristicResult =
            product.SetCharacteristic(
                productType,
                definition,
                characteristicValueResult.Value);

        if (setCharacteristicResult.IsFailure)
        {
            return UnitResult.Failure(
                setCharacteristicResult.Error);
        }

        /*
         * Product уже изменён в памяти.
         * Recorder снимет состояние "после"
         * и добавит ProductAuditEntry в DbContext.
         */
        var auditResult =
            await _auditRecorder
                .RecordManualChangeAsync(
                    product,
                    command.ChangedByUserId,
                    ProductAuditOperation.CharacteristicSet,
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

        /*
         * Изменение Product и audit entry
         * сохраняются одним SaveChanges.
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
