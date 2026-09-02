namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public sealed record CatalogImportRecognitionEnrichmentSummary(
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
    IReadOnlyCollection<CatalogImportRecognitionAppliedValue> AppliedValues)
{
    public static CatalogImportRecognitionEnrichmentSummary Empty { get; } = new(
        RowsAnalyzedCount: 0,
        FilledRowsCount: 0,
        FilledValuesCount: 0,
        BlockedByRecognitionConflictCount: 0,
        BlockedByLowConfidenceCount: 0,
        BlockedByUnsupportedSourceCount: 0,
        BlockedByInvalidExcelValueCount: 0,
        BlockedByInvalidRecognizedValueCount: 0,
        FailedRecognitionRowsCount: 0,
        AppliedValuesDetailsTruncated: false,
        AppliedValues: Array.Empty<CatalogImportRecognitionAppliedValue>());
}