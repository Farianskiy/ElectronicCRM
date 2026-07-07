namespace ElectronicService.Contracts.Catalog.Metadata;

public sealed record CatalogProductTypeCharacteristicResponse(
    Guid Id,
    string Code,
    string Name,
    string DataType,
    string? Unit,
    bool IsRequired,
    bool IsFilterable,
    bool IsUsedForReplacement,
    string ReplacementMatchMode,
    int ReplacementWeight);