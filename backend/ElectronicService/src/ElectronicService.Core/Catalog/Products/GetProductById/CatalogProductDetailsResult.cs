namespace ElectronicService.Core.Catalog.Products.GetProductById;

public sealed record CatalogProductDetailsResult(
    Guid Id,
    string Article,
    string Name,
    Guid ProductTypeId,
    string ProductTypeCode,
    string ProductTypeName,
    Guid ManufacturerId,
    string ManufacturerName,
    decimal PriceAmount,
    string PriceCurrency,
    decimal StockQuantity,
    IReadOnlyCollection<CatalogProductCharacteristicResult> Characteristics,
    IReadOnlyCollection<CatalogProductAliasResult> Aliases);