using ElectronicService.Domain.Catalog.ImportBatches;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public sealed record CatalogImportWorkbookAnalysis(
    IReadOnlyCollection<CatalogImportColumn>
        Columns,
    IReadOnlyCollection<CatalogImportRow>
        Rows,
    bool MappingRequired,
    int ValidRowsCount,
    int ErrorRowsCount);