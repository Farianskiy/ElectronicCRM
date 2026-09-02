using ElectronicService.Core.Catalog.ProductNames.Explanation;

namespace ElectronicService.Core.Catalog.ImportBatches.GetRowExplanations;

public sealed record GetCatalogImportRowExplanationsQuery(
    Guid BatchId,
    Guid CurrentUserId,
    IReadOnlyCollection<Guid> RowIds,
    uint? ExpectedVersion);

public enum CatalogImportRowExplanationStatus
{
    Ready,
    NameMissing,
    ProductTypeRequired,
    NameTooLong,
    RecognitionTimedOut,
    InvalidEvidence
}

public sealed record CatalogImportRowExplanationResult(
    Guid RowId,
    int RowNumber,
    string? ProductName,
    CatalogImportRowExplanationStatus Status,
    bool HasConflicts,
    CatalogProductNameExplanationResult? Explanation);

public sealed record GetCatalogImportRowExplanationsResult(
    Guid BatchId,
    uint BatchVersion,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyCollection<CatalogImportRowExplanationResult> Items);