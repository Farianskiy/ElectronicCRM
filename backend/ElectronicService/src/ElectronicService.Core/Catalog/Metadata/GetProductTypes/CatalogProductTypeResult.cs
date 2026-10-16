namespace ElectronicService.Core.Catalog.Metadata.GetProductTypes;

public sealed record CatalogProductTypeResult(
    Guid Id,
    string Code,
    string Name,
    string Kind = "MainProduct",
    bool IsArchived = false);
