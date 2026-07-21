using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Products
    .RemoveCharacteristic;

public sealed class RemoveProductCharacteristicCommandHandler
{
    private readonly IProductRepository _productRepository;
    private readonly ICatalogProductMetadataRepository
        _metadataRepository;

    public RemoveProductCharacteristicCommandHandler(
        IProductRepository productRepository,
        ICatalogProductMetadataRepository metadataRepository)
    {
        _productRepository = productRepository;
        _metadataRepository = metadataRepository;
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

        var removeResult = product.RemoveCharacteristic(
            productType,
            definition.Id);

        if (removeResult.IsFailure)
        {
            return UnitResult.Failure(removeResult.Error);
        }

        await _productRepository
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return UnitResult.Success<DomainError>();
    }
}