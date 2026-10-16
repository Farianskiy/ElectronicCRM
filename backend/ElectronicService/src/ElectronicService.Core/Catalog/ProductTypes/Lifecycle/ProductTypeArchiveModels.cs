namespace ElectronicService.Core.Catalog.ProductTypes.Lifecycle;

public sealed record ProductTypeArchiveBlocker(
    string Code,
    string Message,
    int Count);

public sealed record ProductTypeArchivePreview(
    Guid ProductTypeId,
    string ProductTypeCode,
    string ProductTypeName,
    bool IsArchived,
    int ProductsCount,
    int CharacteristicsCount,
    bool CanArchive,
    IReadOnlyCollection<ProductTypeArchiveBlocker> Blockers);
