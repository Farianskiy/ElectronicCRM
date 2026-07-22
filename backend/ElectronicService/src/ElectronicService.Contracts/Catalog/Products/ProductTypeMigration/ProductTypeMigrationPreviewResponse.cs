namespace ElectronicService.Contracts.Catalog.Products
    .ProductTypeMigration;

public sealed record ProductTypeMigrationPreviewResponse(
    Guid ProductId,
    uint ProductVersion,

    Guid CurrentProductTypeId,
    string CurrentProductTypeCode,
    string CurrentProductTypeName,

    Guid TargetProductTypeId,
    string TargetProductTypeCode,
    string TargetProductTypeName,

    bool CanApplyWithoutAdditionalValues,

    IReadOnlyCollection<
        ProductTypeMigrationCharacteristicValueResponse>
        PreservedCharacteristics,

    IReadOnlyCollection<
        ProductTypeMigrationCharacteristicValueResponse>
        RemovedCharacteristics,

    IReadOnlyCollection<
        ProductTypeMigrationMissingRequiredCharacteristicResponse>
        MissingRequiredCharacteristics);