namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record StartCatalogImportReviewResponse(
    Guid BatchId,
    string Status,
    Guid? ReviewedByUserId,
    DateTime? ReviewedAtUtc,
    uint Version);