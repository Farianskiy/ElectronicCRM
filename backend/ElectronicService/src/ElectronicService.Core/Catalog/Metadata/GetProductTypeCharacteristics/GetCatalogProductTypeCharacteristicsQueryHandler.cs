using ElectronicService.Core.Catalog.Metadata.Abstractions;

namespace ElectronicService.Core.Catalog.Metadata.GetProductTypeCharacteristics;

public sealed class GetCatalogProductTypeCharacteristicsQueryHandler
{
    private readonly ICatalogMetadataReader _catalogMetadataReader;

    public GetCatalogProductTypeCharacteristicsQueryHandler(
        ICatalogMetadataReader catalogMetadataReader)
    {
        _catalogMetadataReader = catalogMetadataReader;
    }

    public Task<IReadOnlyCollection<CatalogProductTypeCharacteristicResult>> Handle(
        GetCatalogProductTypeCharacteristicsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return _catalogMetadataReader.GetProductTypeCharacteristicsAsync(
            query.ProductTypeCode,
            cancellationToken);
    }
}