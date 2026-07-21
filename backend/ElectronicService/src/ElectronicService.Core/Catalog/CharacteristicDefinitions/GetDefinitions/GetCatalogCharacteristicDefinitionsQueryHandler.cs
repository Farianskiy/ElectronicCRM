using ElectronicService.Core.Catalog
    .CharacteristicDefinitions.Abstractions;

namespace ElectronicService.Core.Catalog
    .CharacteristicDefinitions.GetDefinitions;

public sealed class
    GetCatalogCharacteristicDefinitionsQueryHandler
{
    private readonly
        ICatalogCharacteristicDefinitionsReader _reader;

    public GetCatalogCharacteristicDefinitionsQueryHandler(
        ICatalogCharacteristicDefinitionsReader reader)
    {
        _reader = reader;
    }

    public Task<IReadOnlyCollection<
            CatalogCharacteristicDefinitionResult>>
        Handle(
            GetCatalogCharacteristicDefinitionsQuery query,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return _reader.GetAsync(
            query.Search,
            cancellationToken);
    }
}