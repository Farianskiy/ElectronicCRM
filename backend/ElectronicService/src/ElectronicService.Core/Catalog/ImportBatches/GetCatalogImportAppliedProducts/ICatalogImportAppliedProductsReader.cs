namespace ElectronicService.Core.Catalog.ImportBatches.GetCatalogImportAppliedProducts;

public interface ICatalogImportAppliedProductsReader
{
    Task<CatalogImportAppliedProductsReadResult> ReadAsync(
        Guid batchId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}