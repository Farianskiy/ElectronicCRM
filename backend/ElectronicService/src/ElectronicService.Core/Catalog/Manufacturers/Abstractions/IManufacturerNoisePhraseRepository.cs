using ElectronicService.Domain.Catalog.Manufacturers;

namespace ElectronicService.Core.Catalog.Manufacturers.Abstractions;

public interface IManufacturerNoisePhraseRepository
{
    Task<ManufacturerNoisePhrase?> GetByNormalizedPhraseAsync(string normalizedPhrase, CancellationToken cancellationToken = default);

    Task<bool> ActiveExistsAsync(string normalizedPhrase, CancellationToken cancellationToken = default);

    void Add(ManufacturerNoisePhrase noisePhrase);
}