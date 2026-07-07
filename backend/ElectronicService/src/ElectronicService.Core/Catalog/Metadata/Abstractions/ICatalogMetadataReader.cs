using ElectronicService.Core.Catalog.Metadata.GetManufacturers;
using ElectronicService.Core.Catalog.Metadata.GetProductTypeCharacteristics;
using ElectronicService.Core.Catalog.Metadata.GetProductTypes;

namespace ElectronicService.Core.Catalog.Metadata.Abstractions;

public interface ICatalogMetadataReader
{
    Task<IReadOnlyCollection<CatalogProductTypeResult>> GetProductTypesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogProductTypeCharacteristicResult>> GetProductTypeCharacteristicsAsync(
        string productTypeCode,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogManufacturerResult>> GetManufacturersAsync(
        string? search,
        CancellationToken cancellationToken = default);
}