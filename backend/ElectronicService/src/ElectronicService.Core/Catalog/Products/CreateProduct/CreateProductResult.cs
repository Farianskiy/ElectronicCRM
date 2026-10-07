namespace ElectronicService.Core.Catalog.Products.CreateProduct;

public sealed record CreateProductResult(
    Guid ProductId,
    string Article,
    string Name,
    Guid ProductTypeId,
    Guid ManufacturerId,
    decimal PriceAmount,
    decimal StockQuantity);
