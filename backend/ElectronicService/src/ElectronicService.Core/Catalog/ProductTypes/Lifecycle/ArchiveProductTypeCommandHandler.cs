using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ProductTypes.ChangeKind;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ProductTypes.Lifecycle;

public sealed class ArchiveProductTypeCommandHandler
{
    private readonly ProductTypeArchivePlanner _planner;
    private readonly IProductTypeKindManagementRepository _repository;

    public ArchiveProductTypeCommandHandler(
        ProductTypeArchivePlanner planner,
        IProductTypeKindManagementRepository repository)
    {
        _planner = planner;
        _repository = repository;
    }

    public async Task<Result<ProductTypeArchivePreview, DomainError>> Handle(
        Guid productTypeId,
        CancellationToken cancellationToken = default)
    {
        var preview = await _planner
            .BuildAsync(productTypeId, cancellationToken)
            .ConfigureAwait(false);

        if (preview.IsFailure)
        {
            return preview;
        }

        if (!preview.Value.CanArchive)
        {
            return Result.Failure<ProductTypeArchivePreview, DomainError>(
                CatalogErrors.ProductTypeArchiveIsBlocked());
        }

        var productType = await _repository
            .GetByIdAsync(productTypeId, cancellationToken)
            .ConfigureAwait(false);

        if (productType is null)
        {
            return Result.Failure<ProductTypeArchivePreview, DomainError>(
                CatalogErrors.ProductTypeNotFound(productTypeId.ToString()));
        }

        productType.Archive();
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return preview;
    }
}
