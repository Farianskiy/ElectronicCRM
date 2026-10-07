using ElectronicService.Domain.Catalog.Products;

namespace ElectronicService.Core.Catalog.Products.Abstractions;

public interface IProductRepository
{
    void Add(Product product);

    Task<bool> ExistsByArticleAndManufacturerAsync(
        string article,
        Guid manufacturerId,
        CancellationToken cancellationToken = default);

    Task<Product?> GetByIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<Product?> GetByIdWithDetailsAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Product>> GetByIdsWithDetailsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);

    Task<bool> TrySaveChangesAsync(
        CancellationToken cancellationToken = default);
}
