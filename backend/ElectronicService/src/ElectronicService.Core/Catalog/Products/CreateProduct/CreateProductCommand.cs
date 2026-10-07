namespace ElectronicService.Core.Catalog.Products.CreateProduct;

public sealed record CreateProductCommand(
    Guid CurrentUserId,
    string Article,
    string Name,
    Guid ProductTypeId,
    Guid ManufacturerId,
    decimal PriceAmount,
    decimal StockQuantity,
    IReadOnlyList<CreateProductCharacteristic> Characteristics);

public sealed record CreateProductCharacteristic(
    string Code,
    string Value);
