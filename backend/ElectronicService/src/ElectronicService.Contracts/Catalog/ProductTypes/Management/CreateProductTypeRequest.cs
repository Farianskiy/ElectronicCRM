namespace ElectronicService.Contracts.Catalog.ProductTypes.Management;

public sealed record CreateProductTypeRequest(
    string Code,
    string Name);
