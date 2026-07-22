namespace ElectronicService.Contracts.Catalog.Products
    .ProductTypeMigration;

public sealed record
    ApplyProductTypeMigrationValueRequest(
        Guid DefinitionId,
        string Value);