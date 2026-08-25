using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Domain.Catalog.ImportBatches;

namespace ElectronicService.Core.Catalog.ImportBatches.BulkUpdateCatalogImportRows;

public sealed record BulkUpdateCatalogImportRowsResult(
    IReadOnlyCollection<BulkUpdatedCatalogImportRowResult> Rows,
    CatalogImportBatchStatus BatchStatus,
    int RowsCount,
    int ValidRowsCount,
    int ErrorRowsCount,
    uint Version);

public sealed record BulkUpdatedCatalogImportRowResult(
    Guid RowId,
    int RowNumber,
    CatalogImportRowStatus RowStatus,
    CatalogImportNormalizedRowData Data,
    IReadOnlyCollection<CatalogImportRowIssue> Issues,
    IReadOnlyCollection<CatalogImportRowIssue> Warnings);