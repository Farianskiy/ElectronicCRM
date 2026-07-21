using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Products
    .UpdateGeneralInformation;

public sealed class UpdateProductGeneralInformationCommandHandler
{
    private readonly IProductRepository _productRepository;
    private readonly ICatalogProductMetadataRepository
        _metadataRepository;

    public UpdateProductGeneralInformationCommandHandler(
        IProductRepository productRepository,
        ICatalogProductMetadataRepository metadataRepository)
    {
        _productRepository = productRepository;
        _metadataRepository = metadataRepository;
    }

    public async Task<UnitResult<DomainError>> Handle(
        UpdateProductGeneralInformationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ProductId == Guid.Empty)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(command.ProductId)));
        }

        if (command.ManufacturerId == Guid.Empty)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(command.ManufacturerId)));
        }

        var product = await _productRepository
            .GetByIdAsync(
                command.ProductId,
                cancellationToken)
            .ConfigureAwait(false);

        if (product is null)
        {
            return UnitResult.Failure(
                CatalogErrors.ProductNotFound(
                    command.ProductId.ToString()));
        }

        var manufacturer = await _metadataRepository
            .GetManufacturerByIdAsync(
                command.ManufacturerId,
                cancellationToken)
            .ConfigureAwait(false);

        if (manufacturer is null)
        {
            return UnitResult.Failure(
                CatalogErrors.ManufacturerNotFound(
                    command.ManufacturerId));
        }

        var updateResult = product.UpdateGeneralInformation(
            command.Name,
            command.Article,
            manufacturer.Id);

        if (updateResult.IsFailure)
        {
            return UnitResult.Failure(updateResult.Error);
        }

        await _productRepository
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return UnitResult.Success<DomainError>();
    }
}