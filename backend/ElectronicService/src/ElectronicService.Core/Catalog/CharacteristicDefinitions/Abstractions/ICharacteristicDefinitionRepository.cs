using ElectronicService.Domain.Catalog.Characteristics;

namespace ElectronicService.Core.Catalog
    .CharacteristicDefinitions.Abstractions;

public interface ICharacteristicDefinitionRepository
{
    Task<CharacteristicDefinition?> GetByIdAsync(
        Guid characteristicDefinitionId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByCodeAsync(
        string normalizedCode,
        CancellationToken cancellationToken = default);

    void Add(CharacteristicDefinition definition);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}