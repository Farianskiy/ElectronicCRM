using ElectronicService.Core.Catalog.ProductNames.Explanation;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public enum CatalogImportProductNameExplanationSampleKind
{
    None = 0,
    Unexplained = 1,
    PartiallyExplained = 2,
    FullyExplained = 3
}

public sealed record CatalogImportProductNameExplanationSummary(
    int RowsAnalyzedCount,
    int RowsWithEvidenceCount,
    int FullyExplainedRowsCount,
    int PartiallyExplainedRowsCount,
    int UnexplainedRowsCount,
    decimal AverageCoverage,
    bool SamplesTruncated,
    IReadOnlyCollection<CatalogImportProductNameExplanationSample> Samples);

public sealed record CatalogImportProductNameExplanationSample(
    int RowNumber,
    CatalogImportProductNameExplanationSampleKind Kind,
    CatalogProductNameExplanationResult Explanation,
    int ManufacturerEvidenceCount,
    int ProductTypeEvidenceCount,
    int CharacteristicEvidenceCount)
{
    public bool HasManufacturerEvidence =>
        ManufacturerEvidenceCount > 0;

    public bool HasProductTypeEvidence =>
        ProductTypeEvidenceCount > 0;

    public bool HasCharacteristicEvidence =>
        CharacteristicEvidenceCount > 0;
}