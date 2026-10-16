namespace ElectronicService.Contracts.Catalog.ProductTypes.Management;

public sealed record ProductTypeKindChangeResponse(
    Guid ProductTypeId,
    string ProductTypeCode,
    string ProductTypeName,
    string CurrentKind,
    string TargetKind,
    int ProductsCount,
    int CharacteristicsCount,
    bool CanChange,
    IReadOnlyCollection<ProductTypeKindChangeBlockerResponse> Blockers);

public sealed record ProductTypeKindChangeBlockerResponse(
    string Code,
    string Message,
    int Count);
