namespace ElectronicService.Core.Catalog.Metadata.GetProductTypeCharacteristics;

public sealed record CatalogProductTypeCharacteristicResult(
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