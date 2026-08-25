namespace ElectronicService.Core.Catalog.Manufacturers.Resolution;

public interface IManufacturerResolver
{
    Task<ManufacturerResolutionIndex> LoadIndexAsync(
        CancellationToken cancellationToken = default);
}