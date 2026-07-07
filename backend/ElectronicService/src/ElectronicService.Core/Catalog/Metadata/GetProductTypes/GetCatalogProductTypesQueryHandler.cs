using ElectronicService.Core.Catalog.Metadata.Abstractions;

namespace ElectronicService.Core.Catalog.Metadata.GetProductTypes;

public sealed class GetCatalogProductTypesQueryHandler
{
    private readonly ICatalogMetadataReader _catalogMetadataReader;

    public GetCatalogProductTypesQueryHandler(
        ICatalogMetadataReader catalogMetadataReader)
    {
        _catalogMetadataReader = catalogMetadataReader;
    }

    public Task<IReadOnlyCollection<CatalogProductTypeResult>> Handle(
        GetCatalogProductTypesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return _catalogMetadataReader.GetProductTypesAsync(cancellationToken);
    }
}