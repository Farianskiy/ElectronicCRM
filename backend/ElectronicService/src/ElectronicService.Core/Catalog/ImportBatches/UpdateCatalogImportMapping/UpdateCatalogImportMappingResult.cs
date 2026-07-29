using ElectronicService.Domain.Catalog.ImportBatches;

namespace ElectronicService.Core.Catalog.ImportBatches.UpdateCatalogImportMapping;

public sealed record UpdateCatalogImportMappingResult(
    Guid BatchId,
    CatalogImportBatchStatus Status,
    Guid ProductTypeId,
    int ColumnsCount,
    int UnmappedColumnsCount,
    int UnconfirmedColumnsCount,
    uint Version);