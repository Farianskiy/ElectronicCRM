using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ProductTypes;

namespace ElectronicService.Core.Catalog.Products.Abstractions;

public interface ICatalogProductMetadataRepository
{
    Task<ProductType?> GetProductTypeByIdAsync(
        Guid productTypeId,
        CancellationToken cancellationToken = default);

    Task<CharacteristicDefinition?> GetCharacteristicDefinitionByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);
}