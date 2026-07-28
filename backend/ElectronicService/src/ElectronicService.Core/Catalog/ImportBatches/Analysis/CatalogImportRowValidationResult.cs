using ElectronicService.Domain.Catalog.ImportBatches;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public sealed record CatalogImportRowValidationResult(
    CatalogImportRowStatus Status,
    CatalogImportNormalizedRowData Data,
    IReadOnlyCollection<CatalogImportRowIssue> Issues,
    IReadOnlyCollection<CatalogImportRowIssue> Warnings);