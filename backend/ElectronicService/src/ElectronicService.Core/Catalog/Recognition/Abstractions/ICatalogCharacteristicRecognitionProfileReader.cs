using ElectronicService.Core.Catalog.Recognition.Models;

namespace ElectronicService.Core.Catalog.Recognition.Abstractions;

public interface ICatalogCharacteristicRecognitionProfileReader
{
    Task<IReadOnlyCollection<CatalogCharacteristicRecognitionProfileResult>> GetProfilesAsync(
        Guid productTypeId,
        CancellationToken cancellationToken = default);
}