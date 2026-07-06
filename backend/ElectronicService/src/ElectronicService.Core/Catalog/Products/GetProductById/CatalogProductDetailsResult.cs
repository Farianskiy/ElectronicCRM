namespace ElectronicService.Core.Catalog.Products.GetProductById;

public sealed record CatalogProductDetailsResult(
    Guid Id,
    string Article,
    string Name,
    string ProductTypeCode,
    string ProductTypeName,
    string ManufacturerName,
    decimal PriceAmount,
    string PriceCurrency,
    decimal StockQuantity,
    IReadOnlyCollection<CatalogProductCharacteristicResult> Characteristics);