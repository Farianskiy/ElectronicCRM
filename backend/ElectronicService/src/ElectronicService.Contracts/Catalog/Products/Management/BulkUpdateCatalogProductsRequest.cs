namespace ElectronicService.Contracts.Catalog.Products.Management;

public sealed record BulkUpdateCatalogProductsRequest(
    IReadOnlyCollection<BulkUpdateCatalogProductRequest>? Rows);

public sealed record BulkUpdateCatalogProductRequest(
    Guid ProductId,
    string? Name,
    string? Article,
    Guid? ManufacturerId,
    decimal? PriceAmount,
    string? PriceCurrency,
    decimal? StockQuantity);
