namespace ElectronicService.Contracts.Catalog.Products
    .ProductTypeMigration;

public sealed record ApplyProductTypeMigrationRequest(
    Guid TargetProductTypeId,

    uint ExpectedProductVersion,

    Guid ExpectedCurrentProductTypeId,

    IReadOnlyCollection<Guid>?
        ExpectedRemovedCharacteristicDefinitionIds,

    IReadOnlyCollection<Guid>?
        ExpectedMissingRequiredCharacteristicDefinitionIds,

    IReadOnlyCollection<
        ApplyProductTypeMigrationValueRequest>?
        RequiredValues);