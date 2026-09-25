namespace ElectronicService.Core.Catalog.ProductTypes.CreateProductType;

public sealed record CreateProductTypeCommand(
    string Code,
    string Name);
