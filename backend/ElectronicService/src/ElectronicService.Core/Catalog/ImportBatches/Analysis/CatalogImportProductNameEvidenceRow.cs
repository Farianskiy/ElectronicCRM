using ElectronicService.Core.Catalog.ProductNames.Explanation;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public sealed record CatalogImportProductNameEvidenceRow(
    int RowNumber,
    IReadOnlyCollection<CatalogProductNameEvidenceSpan> Evidence);