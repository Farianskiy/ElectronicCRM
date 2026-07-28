namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record RequestCatalogImportChangesResponse(
    Guid BatchId,
    string Status,
    Guid? ChangesRequestedByUserId,
    DateTime? ChangesRequestedAtUtc,
    string? Comment,
    uint Version);