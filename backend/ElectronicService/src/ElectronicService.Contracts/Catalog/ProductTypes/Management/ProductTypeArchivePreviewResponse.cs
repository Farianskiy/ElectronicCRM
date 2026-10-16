namespace ElectronicService.Contracts.Catalog.ProductTypes.Management;

public sealed record ProductTypeArchivePreviewResponse(
    Guid ProductTypeId,
    string ProductTypeCode,
    string ProductTypeName,
    bool IsArchived,
    int ProductsCount,
    int CharacteristicsCount,
    bool CanArchive,
    IReadOnlyCollection<ProductTypeArchiveBlockerResponse> Blockers);

public sealed record ProductTypeArchiveBlockerResponse(
    string Code,
    string Message,
    int Count);
