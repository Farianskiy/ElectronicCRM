namespace ElectronicService.Contracts.Catalog.Metadata;

public sealed record CatalogProductTypeResponse(
    Guid Id,
    string Code,
    string Name);