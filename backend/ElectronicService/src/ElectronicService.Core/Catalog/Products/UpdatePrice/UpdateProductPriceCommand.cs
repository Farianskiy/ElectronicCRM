namespace ElectronicService.Core.Catalog.Products.UpdatePrice;

public sealed record UpdateProductPriceCommand(
    Guid ProductId,
    Guid ChangedByUserId,
    decimal Amount,
    string Currency);