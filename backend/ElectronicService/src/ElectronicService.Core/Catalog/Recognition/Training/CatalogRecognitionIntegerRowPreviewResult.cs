namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionIntegerRowPreviewResult(
    Guid RowId,
    int RowNumber,
    string? ProductName,
    string Status,
    string? CurrentValue,
    string? ProposedValue,
    IReadOnlyList<CatalogRecognitionIntegerCapture> Captures);