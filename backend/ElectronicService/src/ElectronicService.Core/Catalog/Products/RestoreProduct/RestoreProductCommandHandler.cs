using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Products.RestoreProduct;

public sealed class RestoreProductCommandHandler
{
    private readonly IProductRepository _productRepository;

    public RestoreProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<UnitResult<DomainError>> Handle(
        RestoreProductCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ProductId == Guid.Empty
            || command.RestoredByUserId == Guid.Empty)
        {
            return UnitResult.Failure(
                CatalogErrors.CurrentUserIsRequired());
        }

        var product = await _productRepository
            .GetByIdIncludingArchivedAsync(command.ProductId, cancellationToken)
            .ConfigureAwait(false);

        if (product is null)
        {
            return UnitResult.Failure(
                CatalogErrors.ProductNotFound(command.ProductId.ToString()));
        }

        product.Restore();

        var saved = await _productRepository
            .TrySaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return saved
            ? UnitResult.Success<DomainError>()
            : UnitResult.Failure(
                CatalogErrors.ProductConcurrencyConflict(command.ProductId));
    }
}
