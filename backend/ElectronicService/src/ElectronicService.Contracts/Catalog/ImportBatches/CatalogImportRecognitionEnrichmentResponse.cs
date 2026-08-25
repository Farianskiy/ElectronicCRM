namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record CatalogImportRecognitionEnrichmentResponse(
    int RowsAnalyzedCount,
    int FilledRowsCount,
    int FilledValuesCount,
    int BlockedByRecognitionConflictCount,
    int BlockedByLowConfidenceCount,
    int BlockedByUnsupportedSourceCount,
    int BlockedByInvalidExcelValueCount,
    int BlockedByInvalidRecognizedValueCount,
    int FailedRecognitionRowsCount,
    bool AppliedValuesDetailsTruncated,
    IReadOnlyCollection<CatalogImportRecognitionAppliedValueResponse> AppliedValues);

public sealed record CatalogImportRecognitionAppliedValueResponse(
    int RowNumber,
    Guid CharacteristicDefinitionId,
    string CharacteristicCode,
    string CharacteristicName,
    string Value,
    string RawValue,
    string RecognitionSource,
    decimal Confidence,
    int SpanStart,
    int SpanLength,
    int Priority,
    string RecognizerKey);