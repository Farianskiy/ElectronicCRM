using ElectronicService.Core.Catalog.ProductTypes
    .GetAvailableCharacteristicDefinitions;
using ElectronicService.Core.Catalog.ProductTypes
    .GetCharacteristicSchema;

namespace ElectronicService.Core.Catalog.ProductTypes.Abstractions;

public interface ICatalogProductTypeSchemaReader
{
    Task<CatalogProductTypeCharacteristicSchemaResult?>
        GetByCodeAsync(
            string productTypeCode,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<
            AvailableCharacteristicDefinitionResult>?>
        GetAvailableDefinitionsAsync(
            string productTypeCode,
            string? search,
            CancellationToken cancellationToken = default);
}