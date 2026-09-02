using ElectronicService.Core.Catalog.Manufacturers.Resolution;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public enum CatalogImportManufacturerRecognitionShadowSampleKind
{
    None = 0,
    Conflict = 1,
    NameConflict = 2,
    ComparisonUnavailable = 3,
    Suggestion = 4,
    Match = 5
}

public sealed record CatalogImportManufacturerRecognitionShadowResult(
    int RowsAnalyzedCount,
    int RowsWithExcelManufacturerValueCount,
    int RowsWithResolvedExcelManufacturerCount,
    int RowsWithRecognizedManufacturerCount,
    int MatchesCount,
    int ConflictsCount,
    int SuggestionsCount,
    int NameConflictsCount,
    int NameUnresolvedCount,
    int ComparisonUnavailableCount,
    bool SamplesTruncated,
    IReadOnlyCollection<CatalogImportManufacturerRecognitionShadowSample> Samples,
    IReadOnlyCollection<CatalogImportProductNameEvidenceRow> EvidenceRows);

public sealed record CatalogImportManufacturerRecognitionShadowSample(
    int RowNumber,
    CatalogImportManufacturerRecognitionShadowSampleKind Kind,
    string ProductName,
    Guid? ExcelManufacturerId,
    string? ExcelManufacturerName,
    string? ExcelManufacturerResolutionSource,
    Guid? RecognizedManufacturerId,
    string? RecognizedManufacturerName,
    string? RawRecognizedValue,
    decimal? Confidence,
    ManufacturerResolutionSource? RecognitionSource,
    int? SpanStart,
    int? SpanLength,
    IReadOnlyCollection<CatalogImportManufacturerRecognitionShadowCandidate> Candidates,
    string Details);

public sealed record CatalogImportManufacturerRecognitionShadowCandidate(
    Guid ManufacturerId,
    string ManufacturerName,
    string RawValue,
    string NormalizedValue,
    decimal Confidence,
    ManufacturerResolutionSource Source,
    Guid? ManufacturerAliasId,
    int SpanStart,
    int SpanLength);