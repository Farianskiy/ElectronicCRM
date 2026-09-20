namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionMultiIntegerPart(
    string? Literal,
    Guid? CharacteristicDefinitionId);

public sealed record CatalogRecognitionMultiIntegerPattern(
    IReadOnlyList<CatalogRecognitionMultiIntegerPart> Parts);

public sealed record CatalogRecognitionMultiIntegerCapture(
    Guid CharacteristicDefinitionId,
    string RawValue,
    string NormalizedValue,
    int SpanStart,
    int SpanLength);

public sealed record CatalogRecognitionMultiIntegerMatchResult(
    string Status,
    IReadOnlyList<CatalogRecognitionMultiIntegerCapture> Captures);