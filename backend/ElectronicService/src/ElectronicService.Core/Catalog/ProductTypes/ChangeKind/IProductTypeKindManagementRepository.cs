using ElectronicService.Domain.Catalog.ProductTypes;

namespace ElectronicService.Core.Catalog.ProductTypes.ChangeKind;

public interface IProductTypeKindManagementRepository
{
    Task<ProductType?> GetByIdAsync(
        Guid productTypeId,
        CancellationToken cancellationToken = default);

    Task<ProductTypeKindChangeDependencySnapshot> GetDependenciesAsync(
        Guid productTypeId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
