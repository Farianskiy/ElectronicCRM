namespace ElectronicService.Contracts.Catalog.ProductTypes.Management;

public sealed record CreateProductTypeResponse(
    Guid Id,
    string Code,
    string Name);
