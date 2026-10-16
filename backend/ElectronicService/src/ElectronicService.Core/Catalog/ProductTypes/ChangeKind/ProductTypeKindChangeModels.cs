using ElectronicService.Domain.Catalog.ProductTypes;

namespace ElectronicService.Core.Catalog.ProductTypes.ChangeKind;

public sealed record ProductTypeKindChangeDependencySnapshot(
    int ProductsCount,
    int CharacteristicsCount,
    int ComponentOffersCount,
    int SelectedComponentsCount,
    int DraftCalculationComponentLinesCount,
    int ComponentNeedsCount,
    int DraftMainCalculationLinesCount,
    int ActiveImportBatchesCount);

public sealed record ProductTypeKindChangeBlocker(
    string Code,
    string Message,
    int Count);

public sealed record ProductTypeKindChangePreview(
    Guid ProductTypeId,
    string ProductTypeCode,
    string ProductTypeName,
    ProductTypeKind CurrentKind,
    ProductTypeKind TargetKind,
    int ProductsCount,
    int CharacteristicsCount,
    bool CanChange,
    IReadOnlyCollection<ProductTypeKindChangeBlocker> Blockers);
