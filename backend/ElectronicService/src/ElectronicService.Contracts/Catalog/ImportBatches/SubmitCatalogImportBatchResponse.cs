namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record SubmitCatalogImportBatchResponse(
    Guid BatchId,
    string Status,
    DateTime? SubmittedAtUtc,
    uint Version);