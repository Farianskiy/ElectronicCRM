using ElectronicService.Core.Catalog.Products.GetProducts;

namespace ElectronicService.Core.Catalog.Products.ExportProducts;

public interface ICatalogProductsWorkbookExporter
{
    Task<byte[]> ExportAsync(
        IReadOnlyCollection<CatalogProductListItemResult> products,
        DateTime generatedAtUtc,
        CancellationToken cancellationToken = default);
}
