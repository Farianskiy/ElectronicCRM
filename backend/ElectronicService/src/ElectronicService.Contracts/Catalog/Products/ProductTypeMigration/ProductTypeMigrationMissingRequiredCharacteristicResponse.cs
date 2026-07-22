namespace ElectronicService.Contracts.Catalog.Products
    .ProductTypeMigration;

public sealed record
    ProductTypeMigrationMissingRequiredCharacteristicResponse(
        Guid DefinitionId,
        string Code,
        string Name,
        string DataType,
        string? Unit);