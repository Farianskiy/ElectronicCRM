using ElectronicService.Domain.Catalog.ProductTypes;

namespace ElectronicService.Core.Catalog.ProductTypes.Abstractions;

public interface IProductTypeManagementRepository
{
    Task<bool> ExistsByCodeAsync(
        string normalizedCode,
        CancellationToken cancellationToken = default);

    void Add(ProductType productType);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
