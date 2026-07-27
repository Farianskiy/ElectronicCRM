using ElectronicService.Core.Catalog
    .ImportBatches.Analysis;
using ElectronicService.Domain.Catalog
    .ImportBatches;

namespace ElectronicService.Core.Catalog
    .ImportBatches.GetCatalogImportRows;

public sealed record GetCatalogImportRowsResult(
    IReadOnlyCollection<
        GetCatalogImportRowResult> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record GetCatalogImportRowResult(
    Guid RowId,
    int RowNumber,
    CatalogImportRowStatus Status,
    IReadOnlyDictionary<int, string> RawData,
    CatalogImportNormalizedRowData Data,
    IReadOnlyCollection<
        CatalogImportRowIssue> Issues,
    IReadOnlyCollection<
        CatalogImportRowIssue> Warnings);