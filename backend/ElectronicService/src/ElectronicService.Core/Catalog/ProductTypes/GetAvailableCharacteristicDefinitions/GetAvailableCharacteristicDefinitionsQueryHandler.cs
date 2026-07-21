using ElectronicService.Core.Catalog.ProductTypes.Abstractions;

namespace ElectronicService.Core.Catalog.ProductTypes
    .GetAvailableCharacteristicDefinitions;

public sealed class
    GetAvailableCharacteristicDefinitionsQueryHandler
{
    private readonly ICatalogProductTypeSchemaReader
        _schemaReader;

    public GetAvailableCharacteristicDefinitionsQueryHandler(
        ICatalogProductTypeSchemaReader schemaReader)
    {
        _schemaReader = schemaReader;
    }

    public Task<IReadOnlyCollection<
            AvailableCharacteristicDefinitionResult>?>
        Handle(
            GetAvailableCharacteristicDefinitionsQuery query,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return _schemaReader.GetAvailableDefinitionsAsync(
            query.ProductTypeCode,
            query.Search,
            cancellationToken);
    }
}