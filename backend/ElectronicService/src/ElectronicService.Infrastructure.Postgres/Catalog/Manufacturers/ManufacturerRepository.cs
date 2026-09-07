
using ElectronicService.Core.Catalog.Manufacturers.Abstractions;
using ElectronicService.Domain.Catalog.Manufacturers;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Manufacturers;

public sealed class ManufacturerRepository : IManufacturerRepository
{
    private readonly ElectronicDbContext _dbContext;

    public ManufacturerRepository(ElectronicDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public Task<bool> ExistsByIdAsync(
        Guid manufacturerId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Manufacturers
            .AsNoTracking()
            .AnyAsync(
                manufacturer =>
                    manufacturer.Id == manufacturerId,
                cancellationToken);
    }

    public Task<bool> ExistsByNormalizedNameAsync(
        string normalizedName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedName);

        return _dbContext.Manufacturers
            .AsNoTracking()
            .AnyAsync(
                manufacturer =>
                    manufacturer.NormalizedName == normalizedName,
                cancellationToken);
    }

    public void Add(Manufacturer manufacturer)
    {
        ArgumentNullException.ThrowIfNull(manufacturer);

        _dbContext.Manufacturers.Add(manufacturer);
    }
}