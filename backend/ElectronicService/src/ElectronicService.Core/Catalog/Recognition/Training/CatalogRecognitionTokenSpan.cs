namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionTokenSpan(
    int FirstTokenIndex,
    int TokenCount);