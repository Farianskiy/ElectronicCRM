namespace ElectronicService.Core.Catalog.Products
    .PreviewProductTypeMigration;

public sealed record
    ProductTypeMigrationCharacteristicValueResult(
        Guid DefinitionId,
        string Code,
        string Name,
        string DataType,
        string? Unit,
        string Value);