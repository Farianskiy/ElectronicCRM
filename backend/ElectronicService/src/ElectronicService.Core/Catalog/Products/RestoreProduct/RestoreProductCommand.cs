namespace ElectronicService.Core.Catalog.Products.RestoreProduct;

public sealed record RestoreProductCommand(
    Guid ProductId,
    Guid RestoredByUserId);
