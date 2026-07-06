namespace ElectronicService.Core.Catalog.Products.UpdateStock;

public sealed record UpdateProductStockCommand(
    Guid ProductId,
    decimal Quantity);