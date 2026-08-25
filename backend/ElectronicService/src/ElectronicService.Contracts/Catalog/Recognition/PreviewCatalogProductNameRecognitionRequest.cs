namespace ElectronicService.Contracts.Catalog.Recognition;

public sealed record PreviewCatalogProductNameRecognitionRequest(
    string ProductName,
    string? ProductTypeCode);