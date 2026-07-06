namespace ElectronicService.Contracts.Catalog.Products.Replacements;

public sealed record ProductReplacementItemResponse(
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