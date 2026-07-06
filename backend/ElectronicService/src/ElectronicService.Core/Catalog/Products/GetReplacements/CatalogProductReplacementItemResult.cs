namespace ElectronicService.Core.Catalog.Products.GetReplacements;

public sealed record CatalogProductReplacementItemResult(
    Guid Id,
    string Article,
    string Name,
    string ProductTypeCode,
    string ProductTypeName,
    string ManufacturerName,
    decimal PriceAmount,
    string PriceCurrency,
    decimal StockQuantity,
    decimal ReplacementScore);