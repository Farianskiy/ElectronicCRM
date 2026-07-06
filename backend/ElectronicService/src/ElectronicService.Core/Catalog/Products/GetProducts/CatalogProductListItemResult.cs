namespace ElectronicService.Core.Catalog.Products.GetProducts;

public sealed record CatalogProductListItemResult(
    Guid Id,
    string Article,
    string Name,
    string ProductTypeCode,
    string ProductTypeName,
    string ManufacturerName,
    decimal PriceAmount,
    string PriceCurrency,
    decimal StockQuantity);