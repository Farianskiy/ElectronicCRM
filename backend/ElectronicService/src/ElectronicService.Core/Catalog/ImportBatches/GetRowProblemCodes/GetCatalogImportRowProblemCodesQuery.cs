using ElectronicService.Domain.Catalog.ImportBatches;

namespace ElectronicService.Core.Catalog.ImportBatches
    .GetRowProblemCodes;

public sealed record GetCatalogImportRowProblemCodesQuery(
    Guid BatchId,
    Guid CurrentUserId,
    CatalogImportRowStatus? Status,
    string? Search,
    uint? ExpectedVersion);

public sealed record CatalogImportRowProblemCodeResult(
    string Code,
    int RowsCount,
    int ErrorRowsCount,
    int WarningRowsCount);

public sealed record GetCatalogImportRowProblemCodesResult(
    Guid BatchId,
    uint BatchVersion,
    int TotalRowsCount,
    int ErrorRowsCount,
    int WarningRowsCount,
    IReadOnlyCollection<CatalogImportRowProblemCodeResult> Items);