using ElectronicService.Domain.Catalog.Manufacturers;

namespace ElectronicService.Core.Catalog.Manufacturers.Abstractions;

public interface IManufacturerRepository
{
    Task<bool> ExistsByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default);

    void Add(Manufacturer manufacturer);
}