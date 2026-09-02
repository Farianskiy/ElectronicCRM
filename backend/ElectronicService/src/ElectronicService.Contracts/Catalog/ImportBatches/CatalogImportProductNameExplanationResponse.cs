namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record CatalogImportProductNameExplanationResponse(
    int RowsAnalyzedCount,
    int RowsWithEvidenceCount,
    int FullyExplainedRowsCount,
    int PartiallyExplainedRowsCount,
    int UnexplainedRowsCount,
    decimal AverageCoverage,
    bool SamplesTruncated,
    IReadOnlyCollection<CatalogImportProductNameExplanationSampleResponse> Samples);

public sealed record CatalogImportProductNameExplanationSampleResponse(
    int RowNumber,
    string Kind,
    string ProductName,
    int MeaningfulCharactersCount,
    int CoveredMeaningfulCharactersCount,
    decimal Coverage,
    bool IsFullyExplained,
    bool HasUnexplainedSpans,
    int ManufacturerEvidenceCount,
    int ProductTypeEvidenceCount,
    int CharacteristicEvidenceCount,
    IReadOnlyCollection<CatalogImportProductNameEvidenceSpanResponse> Evidence,
    IReadOnlyCollection<CatalogImportProductNameUnexplainedSpanResponse> UnexplainedSpans);

public sealed record CatalogImportProductNameEvidenceSpanResponse(
    string Kind,
    string TargetCode,
    string TargetValue,
    string RawValue,
    string Source,
    decimal Confidence,
    int Priority,
    int StartIndex,
    int Length,
    int EndIndex);

public sealed record CatalogImportProductNameUnexplainedSpanResponse(
    string RawValue,
    int StartIndex,
    int Length,
    int EndIndex);