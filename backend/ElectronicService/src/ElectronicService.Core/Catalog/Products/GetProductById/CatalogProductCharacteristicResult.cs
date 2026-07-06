namespace ElectronicService.Core.Catalog.Products.GetProductById;

public sealed record CatalogProductCharacteristicResult(
    string Code,
    string Name,
    string DataType,
    string? Unit,
    string Value);