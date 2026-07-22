namespace ElectronicService.Core.Catalog.Products
    .PreviewProductTypeMigration;

public sealed record PreviewProductTypeMigrationQuery(
    Guid ProductId,
    Guid TargetProductTypeId);