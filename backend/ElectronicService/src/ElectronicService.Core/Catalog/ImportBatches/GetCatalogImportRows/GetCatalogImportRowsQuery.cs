using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.Analysis;

namespace ElectronicService.Core.Catalog.ImportBatches.GetCatalogImportRows;

public sealed record GetCatalogImportRowsQuery(
    Guid BatchId,
    Guid CurrentUserId,
    CatalogImportRowStatus? Status,
    string? Search,
    string? IssueCode,
    CatalogImportRowProblemKind? ProblemKind,
    string? ManufacturerGroupKey,
    CatalogImportRowKnowledgeState? KnowledgeState,
    int Page,
    int PageSize);
