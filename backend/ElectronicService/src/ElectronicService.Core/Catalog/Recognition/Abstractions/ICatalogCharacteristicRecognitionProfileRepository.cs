using ElectronicService.Domain.Catalog.Recognition;

namespace ElectronicService.Core.Catalog.Recognition.Abstractions;

public interface ICatalogCharacteristicRecognitionProfileRepository
{
    Task<CatalogCharacteristicRecognitionProfile?> GetByIdAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid productTypeId,
        Guid characteristicDefinitionId,
        CatalogCharacteristicRecognitionStrategyKind strategyKind,
        Guid? excludedProfileId = null,
        CancellationToken cancellationToken = default);

    void Add(CatalogCharacteristicRecognitionProfile profile);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}