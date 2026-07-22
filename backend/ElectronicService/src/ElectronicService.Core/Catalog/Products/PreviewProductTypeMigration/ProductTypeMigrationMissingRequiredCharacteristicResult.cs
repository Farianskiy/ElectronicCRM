namespace ElectronicService.Core.Catalog.Products
    .PreviewProductTypeMigration;

public sealed record
    ProductTypeMigrationMissingRequiredCharacteristicResult(
        Guid DefinitionId,
        string Code,
        string Name,
        string DataType,
        string? Unit);