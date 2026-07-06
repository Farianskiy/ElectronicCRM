using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Catalog.ValueObjects;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Products.UpdatePrice;

public sealed class UpdateProductPriceCommandHandler
{
    private readonly IProductRepository _productRepository;

    public UpdateProductPriceCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<UnitResult<DomainError>> Handle(
        UpdateProductPriceCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ProductId == Guid.Empty)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(nameof(command.ProductId)));
        }

        var product = await _productRepository
            .GetByIdAsync(command.ProductId, cancellationToken)
            .ConfigureAwait(false);

        if (product is null)
        {
            return UnitResult.Failure(
                CatalogErrors.ProductNotFound(command.ProductId.ToString()));
        }

        var priceResult = Money.Create(
            command.Amount,
            command.Currency);

        if (priceResult.IsFailure)
        {
            return UnitResult.Failure(priceResult.Error);
        }

        var changePriceResult = product.ChangePrice(priceResult.Value);

        if (changePriceResult.IsFailure)
        {
            return UnitResult.Failure(changePriceResult.Error);
        }

        await _productRepository
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return UnitResult.Success<DomainError>();
    }
}