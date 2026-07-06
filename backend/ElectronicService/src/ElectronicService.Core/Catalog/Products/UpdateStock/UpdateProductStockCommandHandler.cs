using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Catalog.ValueObjects;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Products.UpdateStock;

public sealed class UpdateProductStockCommandHandler
{
    private readonly IProductRepository _productRepository;

    public UpdateProductStockCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<UnitResult<DomainError>> Handle(
        UpdateProductStockCommand command,
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

        var stockQuantityResult = StockQuantity.Create(command.Quantity);

        if (stockQuantityResult.IsFailure)
        {
            return UnitResult.Failure(stockQuantityResult.Error);
        }

        var changeStockQuantityResult = product.ChangeStockQuantity(
            stockQuantityResult.Value);

        if (changeStockQuantityResult.IsFailure)
        {
            return UnitResult.Failure(changeStockQuantityResult.Error);
        }

        await _productRepository
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return UnitResult.Success<DomainError>();
    }
}