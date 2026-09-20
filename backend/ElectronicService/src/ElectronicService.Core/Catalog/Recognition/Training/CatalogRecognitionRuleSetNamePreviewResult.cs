namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionRuleSetCharacteristicDescription(
    Guid CharacteristicDefinitionId,
    string Name,
    string? Unit,
    ElectronicService.Domain.Catalog.Characteristics.CharacteristicDataType DataType);

public sealed record CatalogRecognitionRuleSetNamePreviewResult(
    Guid VersionId,
    int VersionNumber,
    Guid ManufacturerId,
    Guid ProductTypeId,
    string ProductName,
    DateTime CheckedAtUtc,
    CatalogRecognitionRuleSetValueResolution Resolution)
{
    public IReadOnlyList<CatalogRecognitionRuleSetCharacteristicDescription>
        CharacteristicDefinitions
    { get; init; } =
            Array.Empty<CatalogRecognitionRuleSetCharacteristicDescription>();
}