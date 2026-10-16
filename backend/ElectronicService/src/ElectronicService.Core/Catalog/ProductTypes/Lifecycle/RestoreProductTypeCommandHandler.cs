using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ProductTypes.ChangeKind;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ProductTypes.Lifecycle;

public sealed class RestoreProductTypeCommandHandler
{
    private readonly IProductTypeKindManagementRepository _repository;

    public RestoreProductTypeCommandHandler(
        IProductTypeKindManagementRepository repository)
    {
        _repository = repository;
    }

    public async Task<UnitResult<DomainError>> Handle(
        Guid productTypeId,
        CancellationToken cancellationToken = default)
    {
        var productType = await _repository
            .GetByIdAsync(productTypeId, cancellationToken)
            .ConfigureAwait(false);

        if (productType is null)
        {
            return UnitResult.Failure(
                CatalogErrors.ProductTypeNotFound(productTypeId.ToString()));
        }

        productType.Restore();
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return UnitResult.Success<DomainError>();
    }
}
