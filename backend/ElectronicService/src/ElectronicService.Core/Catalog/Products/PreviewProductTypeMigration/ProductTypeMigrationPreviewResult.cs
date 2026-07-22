namespace ElectronicService.Core.Catalog.Products
    .PreviewProductTypeMigration;

public sealed record ProductTypeMigrationPreviewResult(
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
        ProductTypeMigrationCharacteristicValueResult>
        PreservedCharacteristics,

    IReadOnlyCollection<
        ProductTypeMigrationCharacteristicValueResult>
        RemovedCharacteristics,

    IReadOnlyCollection<
        ProductTypeMigrationMissingRequiredCharacteristicResult>
        MissingRequiredCharacteristics);