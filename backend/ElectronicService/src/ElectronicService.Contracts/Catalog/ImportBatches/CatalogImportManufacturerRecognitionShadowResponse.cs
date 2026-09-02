namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record CatalogImportManufacturerRecognitionShadowResponse(
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
    IReadOnlyCollection<CatalogImportManufacturerRecognitionShadowSampleResponse> Samples);

public sealed record CatalogImportManufacturerRecognitionShadowSampleResponse(
    int RowNumber,
    string Kind,
    string ProductName,
    Guid? ExcelManufacturerId,
    string? ExcelManufacturerName,
    string? ExcelManufacturerResolutionSource,
    Guid? RecognizedManufacturerId,
    string? RecognizedManufacturerName,
    string? RawRecognizedValue,
    decimal? Confidence,
    string? RecognitionSource,
    int? SpanStart,
    int? SpanLength,
    IReadOnlyCollection<CatalogImportManufacturerRecognitionShadowCandidateResponse> Candidates,
    string Details);

public sealed record CatalogImportManufacturerRecognitionShadowCandidateResponse(
    Guid ManufacturerId,
    string ManufacturerName,
    string RawValue,
    string NormalizedValue,
    decimal Confidence,
    string Source,
    Guid? ManufacturerAliasId,
    int SpanStart,
    int SpanLength);