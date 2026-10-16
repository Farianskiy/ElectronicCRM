namespace ElectronicService.Core.Catalog.Products.ArchiveProduct;

public sealed record ArchiveProductCommand(
    Guid ProductId,
    Guid ArchivedByUserId,
    string? Reason);
