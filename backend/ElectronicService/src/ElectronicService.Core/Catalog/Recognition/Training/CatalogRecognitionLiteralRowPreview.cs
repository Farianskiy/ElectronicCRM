namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionLiteralRowPreviewInput(
    Guid RowId,
    int RowNumber,
    string? ProductName,
    Guid? ManufacturerId,
    Guid? ProductTypeId,
    string? CurrentValue);

public sealed record CatalogRecognitionLiteralRowPreviewResult(
    Guid RowId,
    int RowNumber,
    string? ProductName,
    string Status,
    string? CurrentValue,
    string? ProposedValue,
    IReadOnlyList<int> MatchPositions);