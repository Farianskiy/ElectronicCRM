using ElectronicService.Domain.Catalog.ImportBatches;

namespace ElectronicService.Core.Catalog.ImportBatches.GetCatalogImportRows;

public sealed record GetCatalogImportRowsQuery(
    Guid BatchId,
    Guid CurrentUserId,
    CatalogImportRowStatus? Status,
    string? Search,
    string? IssueCode,
    CatalogImportRowProblemKind? ProblemKind,
    string? ManufacturerGroupKey,
    int Page,
    int PageSize);