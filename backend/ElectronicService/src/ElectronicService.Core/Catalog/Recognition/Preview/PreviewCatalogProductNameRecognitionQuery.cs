namespace ElectronicService.Core.Catalog.Recognition.Preview;

public sealed record PreviewCatalogProductNameRecognitionQuery(
    string ProductName,
    string? ProductTypeCode);