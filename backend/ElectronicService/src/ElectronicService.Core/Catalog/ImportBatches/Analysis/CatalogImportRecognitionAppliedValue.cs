using ElectronicService.Core.Catalog.Recognition.Models;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public sealed record CatalogImportRecognitionAppliedValue(
    int RowNumber,
    Guid CharacteristicDefinitionId,
    string CharacteristicCode,
    string CharacteristicName,
    string Value,
    string RawValue,
    CatalogRecognitionSource RecognitionSource,
    decimal Confidence,
    int SpanStart,
    int SpanLength,
    int Priority,
    string RecognizerKey);