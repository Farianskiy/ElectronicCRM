using CSharpFunctionalExtensions;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ProductTypes.ChangeKind;

public sealed class ProductTypeKindChangePlanner
{
    private readonly IProductTypeKindManagementRepository _repository;

    public ProductTypeKindChangePlanner(
        IProductTypeKindManagementRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ProductTypeKindChangePreview, DomainError>> BuildAsync(
        Guid productTypeId,
        ProductTypeKind targetKind,
        CancellationToken cancellationToken = default)
    {
        if (productTypeId == Guid.Empty)
        {
            return Result.Failure<ProductTypeKindChangePreview, DomainError>(
                CatalogErrors.ProductTypeNotFound(productTypeId.ToString()));
        }

        if (!Enum.IsDefined(targetKind))
        {
            return Result.Failure<ProductTypeKindChangePreview, DomainError>(
                GeneralErrors.ValueIsInvalid(nameof(targetKind)));
        }

        var productType = await _repository
            .GetByIdAsync(productTypeId, cancellationToken)
            .ConfigureAwait(false);

        if (productType is null)
        {
            return Result.Failure<ProductTypeKindChangePreview, DomainError>(
                CatalogErrors.ProductTypeNotFound(productTypeId.ToString()));
        }

        if (productType.Kind == targetKind)
        {
            return Result.Failure<ProductTypeKindChangePreview, DomainError>(
                CatalogErrors.ProductTypeKindIsAlreadySet(targetKind.ToString()));
        }

        if (productType.IsArchived)
        {
            return Result.Failure<ProductTypeKindChangePreview, DomainError>(
                CatalogErrors.ProductTypeIsArchived());
        }

        var dependencies = await _repository
            .GetDependenciesAsync(productTypeId, cancellationToken)
            .ConfigureAwait(false);

        var blockers = BuildBlockers(productType.Kind, targetKind, dependencies);

        return Result.Success<ProductTypeKindChangePreview, DomainError>(
            new ProductTypeKindChangePreview(
                productType.Id,
                productType.Code,
                productType.Name,
                productType.Kind,
                targetKind,
                dependencies.ProductsCount,
                dependencies.CharacteristicsCount,
                blockers.Count == 0,
                blockers));
    }

    private static List<ProductTypeKindChangeBlocker> BuildBlockers(
        ProductTypeKind currentKind,
        ProductTypeKind targetKind,
        ProductTypeKindChangeDependencySnapshot dependencies)
    {
        var blockers = new List<ProductTypeKindChangeBlocker>();

        if (currentKind == ProductTypeKind.Component
            && targetKind == ProductTypeKind.MainProduct)
        {
            AddIfPresent(
                blockers,
                "component_offers",
                "Товары этого типа используются в правилах совместимости как комплектующие.",
                dependencies.ComponentOffersCount);
            AddIfPresent(
                blockers,
                "selected_components",
                "Товары этого типа уже выбраны комплектующими у основных товаров.",
                dependencies.SelectedComponentsCount);
            AddIfPresent(
                blockers,
                "draft_calculation_components",
                "Товары этого типа добавлены комплектующими в незавершённые расчёты цен.",
                dependencies.DraftCalculationComponentLinesCount);
        }

        if (currentKind == ProductTypeKind.MainProduct
            && targetKind == ProductTypeKind.Component)
        {
            AddIfPresent(
                blockers,
                "component_needs",
                "Для этого типа настроены потребности в комплектующих.",
                dependencies.ComponentNeedsCount);
            AddIfPresent(
                blockers,
                "draft_main_calculation_lines",
                "Товары этого типа используются как основные в незавершённых расчётах цен.",
                dependencies.DraftMainCalculationLinesCount);
        }

        return blockers;
    }

    private static void AddIfPresent(
        List<ProductTypeKindChangeBlocker> blockers,
        string code,
        string message,
        int count)
    {
        if (count > 0)
        {
            blockers.Add(new ProductTypeKindChangeBlocker(code, message, count));
        }
    }
}
