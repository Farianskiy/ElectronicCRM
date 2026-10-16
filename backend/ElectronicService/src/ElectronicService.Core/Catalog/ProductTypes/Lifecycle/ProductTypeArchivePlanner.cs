using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ProductTypes.ChangeKind;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ProductTypes.Lifecycle;

public sealed class ProductTypeArchivePlanner
{
    private readonly IProductTypeKindManagementRepository _repository;

    public ProductTypeArchivePlanner(
        IProductTypeKindManagementRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ProductTypeArchivePreview, DomainError>> BuildAsync(
        Guid productTypeId,
        CancellationToken cancellationToken = default)
    {
        var productType = await _repository
            .GetByIdAsync(productTypeId, cancellationToken)
            .ConfigureAwait(false);

        if (productType is null)
        {
            return Result.Failure<ProductTypeArchivePreview, DomainError>(
                CatalogErrors.ProductTypeNotFound(productTypeId.ToString()));
        }

        var dependencies = await _repository
            .GetDependenciesAsync(productTypeId, cancellationToken)
            .ConfigureAwait(false);
        var blockers = new List<ProductTypeArchiveBlocker>();

        AddIfPresent(
            blockers,
            "active_products",
            "У типа есть активные товары. Сначала перенесите или архивируйте их.",
            dependencies.ProductsCount);
        AddIfPresent(
            blockers,
            "component_needs",
            "Тип используется в настройках потребностей комплектующих.",
            dependencies.ComponentNeedsCount);
        AddIfPresent(
            blockers,
            "active_imports",
            "Тип выбран в незавершённых импортах.",
            dependencies.ActiveImportBatchesCount);

        return Result.Success<ProductTypeArchivePreview, DomainError>(
            new ProductTypeArchivePreview(
                productType.Id,
                productType.Code,
                productType.Name,
                productType.IsArchived,
                dependencies.ProductsCount,
                dependencies.CharacteristicsCount,
                !productType.IsArchived && blockers.Count == 0,
                blockers));
    }

    private static void AddIfPresent(
        List<ProductTypeArchiveBlocker> blockers,
        string code,
        string message,
        int count)
    {
        if (count > 0)
        {
            blockers.Add(new ProductTypeArchiveBlocker(code, message, count));
        }
    }
}
