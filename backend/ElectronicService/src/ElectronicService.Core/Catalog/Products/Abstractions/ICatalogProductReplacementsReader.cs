using ElectronicService.Core.Catalog.Products.GetReplacements;

namespace ElectronicService.Core.Catalog.Products.Abstractions;

public interface ICatalogProductReplacementsReader
{
    Task<CatalogProductReplacementsResult?> GetReplacementsAsync(
        GetProductReplacementsQuery query,
        CancellationToken cancellationToken = default);
}