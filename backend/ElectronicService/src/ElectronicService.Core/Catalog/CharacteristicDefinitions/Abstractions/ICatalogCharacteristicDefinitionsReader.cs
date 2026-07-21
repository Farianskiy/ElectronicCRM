using ElectronicService.Core.Catalog
    .CharacteristicDefinitions.GetDefinitions;

namespace ElectronicService.Core.Catalog
    .CharacteristicDefinitions.Abstractions;

public interface ICatalogCharacteristicDefinitionsReader
{
    Task<IReadOnlyCollection<
            CatalogCharacteristicDefinitionResult>>
        GetAsync(
            string? search,
            CancellationToken cancellationToken = default);
}