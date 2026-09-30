namespace ElectronicService.Core.Catalog.ImportBatches.ApplyCatalogImportBatch;

public sealed record CatalogImportApplyExecutionResult(
    int CreatedProductsCount,
    int UpdatedProductsCount);
