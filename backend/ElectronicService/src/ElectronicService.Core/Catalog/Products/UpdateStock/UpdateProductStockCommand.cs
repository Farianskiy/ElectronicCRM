namespace ElectronicService.Core.Catalog.Products.UpdateStock;

public sealed record UpdateProductStockCommand(
    Guid ProductId,
    Guid ChangedByUserId,
    decimal Quantity);