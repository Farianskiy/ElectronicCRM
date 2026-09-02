using ElectronicService.Core.Catalog.Recognition.Models;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public enum CatalogImportRecognitionShadowSampleKind
{
    None = 0,
    Conflict = 1,
    NotRecognized = 2,
    RecognitionWithoutExplicitValue = 3,
    Ambiguous = 4
}

public sealed record CatalogImportRecognitionShadowResult(
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
    IReadOnlyCollection<CatalogImportRecognitionShadowCharacteristicStatistics> Characteristics,
    IReadOnlyCollection<CatalogImportRecognitionShadowConflictGroup> ConflictGroups,
    IReadOnlyCollection<CatalogImportRecognitionShadowSample> Samples,
    IReadOnlyCollection<CatalogImportProductNameEvidenceRow> EvidenceRows);

public sealed record CatalogImportRecognitionShadowCharacteristicStatistics(
    string CharacteristicCode,
    string CharacteristicName,
    int ExplicitValuesCount,
    int RecognizedValuesCount,
    int MatchesCount,
    int ConflictsCount,
    int NotRecognizedCount,
    int RecognitionWithoutExplicitValueCount,
    int AmbiguousCount);

public sealed record CatalogImportRecognitionShadowConflictGroup(
    string CharacteristicCode,
    string CharacteristicName,
    string ExcelValue,
    string RecognizedValue,
    string RawRecognizedValue,
    CatalogRecognitionSource RecognitionSource,
    decimal Confidence,
    int SpanStart,
    int SpanLength,
    int Priority,
    string RecognizerKey,
    int OccurrenceCount,
    IReadOnlyCollection<int> ExampleRowNumbers,
    IReadOnlyCollection<string> ExampleProductNames);

public sealed record CatalogImportRecognitionShadowSample(
    int RowNumber,
    CatalogImportRecognitionShadowSampleKind Kind,
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