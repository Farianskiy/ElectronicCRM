namespace ElectronicService.Contracts.Catalog.Products.Management;

public sealed record CreateCatalogProductResponse(
    Guid ProductId,
    string Article,
    string Name,
    Guid ProductTypeId,
    Guid ManufacturerId,
    decimal PriceAmount,
    decimal StockQuantity);
