namespace ElectronicService.Contracts.Catalog.Products
    .ProductTypeMigration;

public sealed record
    ProductTypeMigrationCharacteristicValueResponse(
        Guid DefinitionId,
        string Code,
        string Name,
        string DataType,
        string? Unit,
        string Value);