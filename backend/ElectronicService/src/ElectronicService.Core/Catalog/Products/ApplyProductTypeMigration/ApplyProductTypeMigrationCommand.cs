namespace ElectronicService.Core.Catalog.Products
    .ApplyProductTypeMigration;

public sealed record ApplyProductTypeMigrationCommand(
    Guid ProductId,
    Guid ChangedByUserId,

    Guid TargetProductTypeId,
    uint ExpectedProductVersion,
    Guid ExpectedCurrentProductTypeId,

    IReadOnlyCollection<Guid>
        ExpectedRemovedCharacteristicDefinitionIds,

    IReadOnlyCollection<Guid>
        ExpectedMissingRequiredCharacteristicDefinitionIds,

    IReadOnlyCollection<
        ApplyProductTypeMigrationValueCommand>
        RequiredValues);