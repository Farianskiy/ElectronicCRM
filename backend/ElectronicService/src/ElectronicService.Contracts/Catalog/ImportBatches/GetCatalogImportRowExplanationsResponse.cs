namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record GetCatalogImportRowExplanationsResponse(
    Guid BatchId,
    uint BatchVersion,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyCollection<CatalogImportRowExplanationResponse> Items);

public sealed record CatalogImportRowExplanationResponse(
    Guid RowId,
    int RowNumber,
    string? ProductName,
    string Status,
    bool HasConflicts,
    CatalogImportProductNameExplanationSampleResponse? Explanation);