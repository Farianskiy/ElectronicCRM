namespace ElectronicService.Contracts.Catalog.Products.Management;

public sealed record CreateCatalogProductRequest(
    string Article,
    string Name,
    Guid ProductTypeId,
    Guid ManufacturerId,
    decimal PriceAmount,
    decimal StockQuantity,
    IReadOnlyList<CreateCatalogProductCharacteristicRequest>? Characteristics);

public sealed record CreateCatalogProductCharacteristicRequest(
    string Code,
    string Value);
