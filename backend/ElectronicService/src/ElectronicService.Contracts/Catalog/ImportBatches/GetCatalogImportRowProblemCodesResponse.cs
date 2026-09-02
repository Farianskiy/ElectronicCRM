namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record GetCatalogImportRowProblemCodesResponse(
    Guid BatchId,
    uint BatchVersion,
    int TotalRowsCount,
    int ErrorRowsCount,
    int WarningRowsCount,
    IReadOnlyCollection<CatalogImportRowProblemCodeResponse> Items);

public sealed record CatalogImportRowProblemCodeResponse(
    string Code,
    int RowsCount,
    int ErrorRowsCount,
    int WarningRowsCount);