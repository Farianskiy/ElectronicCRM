using ElectronicService.Domain.Catalog.Manufacturers;

namespace ElectronicService.Core.Catalog.Manufacturers.Abstractions;

public interface IManufacturerAliasRepository
{
    Task<Manufacturer?> GetManufacturerByIdAsync(Guid manufacturerId, CancellationToken cancellationToken = default);

    Task<bool> ManufacturerNameExistsAsync(string normalizedName, CancellationToken cancellationToken = default);

    Task<bool> ActiveAliasExistsAsync(string normalizedPhrase, CancellationToken cancellationToken = default);

    void Add(ManufacturerAlias manufacturerAlias);
}