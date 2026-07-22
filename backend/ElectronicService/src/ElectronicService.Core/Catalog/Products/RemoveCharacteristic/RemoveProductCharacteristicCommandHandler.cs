using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.Products.Audit;
using ElectronicService.Domain.Catalog.Audit;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Products
    .RemoveCharacteristic;

public sealed class RemoveProductCharacteristicCommandHandler
{
    private readonly IProductRepository
        _productRepository;

    private readonly ICatalogProductMetadataRepository
        _metadataRepository;

    private readonly ProductAuditRecorder
        _auditRecorder;

    public RemoveProductCharacteristicCommandHandler(
        IProductRepository productRepository,
        ICatalogProductMetadataRepository metadataRepository,
        ProductAuditRecorder auditRecorder)
    {
        _productRepository = productRepository;
        _metadataRepository = metadataRepository;
        _auditRecorder = auditRecorder;
    }

    public async Task<UnitResult<DomainError>> Handle(
        RemoveProductCharacteristicCommand command,
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
                GeneralErrors.ValueIsRequired(
                    nameof(command.Code)));
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

        /*
         * Команда получает Code.
         * Handler переводит Code в definition.Id,
         * потому что доменная модель работает
         * со стабильным Guid.
         */
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

        var beforeJson =
            ProductAuditSnapshotSerializer.Serialize(
                product);

        var removeResult =
            product.RemoveCharacteristic(
                productType,
                definition.Id);

        if (removeResult.IsFailure)
        {
            return UnitResult.Failure(
                removeResult.Error);
        }

        var auditResult =
            _auditRecorder.RecordManualChange(
                product,
                command.ChangedByUserId,
                ProductAuditOperation
                    .CharacteristicRemoved,
                beforeJson);

        if (auditResult.IsFailure)
        {
            return UnitResult.Failure(
                auditResult.Error);
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