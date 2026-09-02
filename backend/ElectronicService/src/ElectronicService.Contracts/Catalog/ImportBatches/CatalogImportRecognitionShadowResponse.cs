namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record CatalogImportRecognitionShadowResponse(
    int RowsAnalyzed,
    int RowsWithRecognition,
    int FailedRowsCount,
    int ExplicitValuesCount,
    int RecognizedValuesCount,
    int MatchesCount,
    int ConflictsCount,
    int NotRecognizedCount,
    int RecognitionWithoutExplicitValueCount,
    int AmbiguousCount,
    IReadOnlyCollection<CatalogImportRecognitionShadowCharacteristicResponse> Characteristics,
    IReadOnlyCollection<CatalogImportRecognitionShadowConflictGroupResponse> ConflictGroups,
    IReadOnlyCollection<CatalogImportRecognitionShadowSampleResponse> Samples);

public sealed record CatalogImportRecognitionShadowCharacteristicResponse(
    string CharacteristicCode,
    string CharacteristicName,
    int ExplicitValuesCount,
    int RecognizedValuesCount,
    int MatchesCount,
    int ConflictsCount,
    int NotRecognizedCount,
    int RecognitionWithoutExplicitValueCount,
    int AmbiguousCount);

public sealed record CatalogImportRecognitionShadowConflictGroupResponse(
    string CharacteristicCode,
    string CharacteristicName,
    string ExcelValue,
    string RecognizedValue,
    string RawRecognizedValue,
    string RecognitionSource,
    decimal Confidence,
    int SpanStart,
    int SpanLength,
    int Priority,
    string RecognizerKey,
    int OccurrenceCount,
    IReadOnlyCollection<int> ExampleRowNumbers,
    IReadOnlyCollection<string> ExampleProductNames);

public sealed record CatalogImportRecognitionShadowSampleResponse(
    int RowNumber,
    string Kind,
    string CharacteristicCode,
    string CharacteristicName,
    string ProductName,
    string? ExcelValue,
    string? RecognizedValue,
    string? RawRecognizedValue,
    decimal? Confidence,
    string? RecognitionSource,
    string? RecognizerKey,
    int? SpanStart,
    int? SpanLength,
    int? Priority,
    string? Details);