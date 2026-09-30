namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record ApplyCatalogImportBatchResponse(
    Guid BatchId,
    string Status,
    Guid? AppliedByUserId,
    DateTime? AppliedAtUtc,
    int CreatedProductsCount,
    int UpdatedProductsCount,
    uint Version);
