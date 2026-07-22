namespace ElectronicService.Contracts.Catalog.Products
    .ProductTypeMigration;

public sealed record PreviewProductTypeMigrationRequest(
    Guid TargetProductTypeId);