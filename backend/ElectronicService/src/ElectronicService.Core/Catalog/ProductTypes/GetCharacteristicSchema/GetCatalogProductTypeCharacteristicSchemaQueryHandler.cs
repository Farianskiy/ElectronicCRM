using ElectronicService.Core.Catalog.ProductTypes.Abstractions;

namespace ElectronicService.Core.Catalog.ProductTypes
    .GetCharacteristicSchema;

public sealed class
    GetCatalogProductTypeCharacteristicSchemaQueryHandler
{
    private readonly ICatalogProductTypeSchemaReader
        _schemaReader;

    public GetCatalogProductTypeCharacteristicSchemaQueryHandler(
        ICatalogProductTypeSchemaReader schemaReader)
    {
        _schemaReader = schemaReader;
    }

    public Task<
        CatalogProductTypeCharacteristicSchemaResult?>
        Handle(
            GetCatalogProductTypeCharacteristicSchemaQuery query,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return _schemaReader.GetByCodeAsync(
            query.ProductTypeCode,
            cancellationToken);
    }
}