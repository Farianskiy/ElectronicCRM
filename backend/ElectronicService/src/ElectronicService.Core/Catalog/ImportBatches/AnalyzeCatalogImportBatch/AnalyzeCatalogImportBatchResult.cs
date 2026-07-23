using ElectronicService.Domain.Catalog.ImportBatches;

namespace ElectronicService.Core.Catalog.ImportBatches.AnalyzeCatalogImportBatch;

public sealed record AnalyzeCatalogImportBatchResult(
    Guid BatchId,
    CatalogImportBatchStatus Status,
    Guid? ProductTypeId,
    int ColumnsCount,
    int UnmappedColumnsCount,
    int UnconfirmedColumnsCount,
    int RowsCount,
    int ValidRowsCount,
    int ErrorRowsCount);