namespace ElectronicService.Core.Catalog.Products
    .ApplyProductTypeMigration;

public sealed record
    ApplyProductTypeMigrationValueCommand(
        Guid DefinitionId,
        string Value);