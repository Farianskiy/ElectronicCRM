namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionMultiIntegerRowPreviewInput(
    Guid RowId,
    int RowNumber,
    string? ProductName,
    Guid? ManufacturerId,
    Guid? ProductTypeId,
    IReadOnlyDictionary<string, string> Characteristics);

public sealed record CatalogRecognitionMultiIntegerFieldPreview(
    Guid CharacteristicDefinitionId,
    string Status,
    string? CurrentValue,
    string? ProposedValue,
    CatalogRecognitionMultiIntegerCapture? Capture);

public sealed record CatalogRecognitionMultiIntegerRowPreviewResult(
    Guid RowId,
    int RowNumber,
    string? ProductName,
    string Status,
    IReadOnlyList<CatalogRecognitionMultiIntegerFieldPreview> Fields);