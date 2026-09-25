namespace ElectronicService.Core.Catalog.ProductTypes.CreateProductType;

public sealed record CreateProductTypeResult(
    Guid Id,
    string Code,
    string Name);
