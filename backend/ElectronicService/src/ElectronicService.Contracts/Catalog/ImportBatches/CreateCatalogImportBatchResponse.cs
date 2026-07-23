namespace ElectronicService.Contracts.Catalog
    .ImportBatches;

public sealed record
    CreateCatalogImportBatchResponse(
        Guid BatchId,
        string Status);