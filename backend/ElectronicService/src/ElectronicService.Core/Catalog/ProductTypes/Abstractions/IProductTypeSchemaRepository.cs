using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ProductTypes;

namespace ElectronicService.Core.Catalog.ProductTypes.Abstractions;

public interface IProductTypeSchemaRepository
{
    Task<ProductType?> GetByCodeWithCharacteristicsAsync(
        string productTypeCode,
        CancellationToken cancellationToken = default);

    Task<CharacteristicDefinition?> GetDefinitionByIdAsync(
        Guid characteristicDefinitionId,
        CancellationToken cancellationToken = default);

    Task<int> CountProductsWithoutCharacteristicAsync(
        Guid productTypeId,
        Guid characteristicDefinitionId,
        CancellationToken cancellationToken = default);

    Task<int> CountProductsWithCharacteristicAsync(
        Guid productTypeId,
        Guid characteristicDefinitionId,
        CancellationToken cancellationToken = default);

    void MarkCharacteristicForRemoval(
        ProductTypeCharacteristic characteristic);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}