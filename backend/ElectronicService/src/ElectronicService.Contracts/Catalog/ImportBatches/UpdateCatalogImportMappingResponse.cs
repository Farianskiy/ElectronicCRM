namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record UpdateCatalogImportMappingResponse(
    Guid BatchId,
    string Status,
    Guid ProductTypeId,
    int ColumnsCount,
    int UnmappedColumnsCount,
    int UnconfirmedColumnsCount,
    uint Version);