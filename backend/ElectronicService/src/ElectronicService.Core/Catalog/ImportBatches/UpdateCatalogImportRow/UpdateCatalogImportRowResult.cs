using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Domain.Catalog.ImportBatches;

namespace ElectronicService.Core.Catalog.ImportBatches.UpdateCatalogImportRow;

public sealed record UpdateCatalogImportRowResult(
    Guid RowId,
    int RowNumber,
    CatalogImportRowStatus RowStatus,
    CatalogImportNormalizedRowData Data,
    IReadOnlyCollection<CatalogImportRowIssue> Issues,
    IReadOnlyCollection<CatalogImportRowIssue> Warnings,
    CatalogImportBatchStatus BatchStatus,
    int RowsCount,
    int ValidRowsCount,
    int ErrorRowsCount,
    uint Version);