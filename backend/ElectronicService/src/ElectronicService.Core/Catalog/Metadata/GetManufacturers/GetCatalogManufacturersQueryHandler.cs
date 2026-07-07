using ElectronicService.Core.Catalog.Metadata.Abstractions;

namespace ElectronicService.Core.Catalog.Metadata.GetManufacturers;

public sealed class GetCatalogManufacturersQueryHandler
{
    private readonly ICatalogMetadataReader _catalogMetadataReader;

    public GetCatalogManufacturersQueryHandler(
        ICatalogMetadataReader catalogMetadataReader)
    {
        _catalogMetadataReader = catalogMetadataReader;
    }

    public Task<IReadOnlyCollection<CatalogManufacturerResult>> Handle(
        GetCatalogManufacturersQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return _catalogMetadataReader.GetManufacturersAsync(
            query.Search,
            cancellationToken);
    }
}