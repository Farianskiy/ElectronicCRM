using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Domain.Catalog.Characteristics;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public sealed record CatalogImportWorkbookAnalysis(
    IReadOnlyCollection<CatalogImportColumn>
        Columns,
    IReadOnlyCollection<CatalogImportRow>
        Rows,
    bool MappingRequired,
    int ValidRowsCount,
    int ErrorRowsCount);
