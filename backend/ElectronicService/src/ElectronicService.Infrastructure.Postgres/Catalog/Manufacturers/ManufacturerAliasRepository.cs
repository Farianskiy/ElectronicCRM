using ElectronicService.Core.Catalog.Manufacturers.Abstractions;
using ElectronicService.Domain.Catalog.Manufacturers;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Manufacturers;

public sealed class ManufacturerAliasRepository : IManufacturerAliasRepository
{
    private readonly ElectronicDbContext _dbContext;

    public ManufacturerAliasRepository(ElectronicDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public Task<Manufacturer?> GetManufacturerByIdAsync(Guid manufacturerId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Manufacturers
            .AsNoTracking()
            .SingleOrDefaultAsync(manufacturer => manufacturer.Id == manufacturerId, cancellationToken);
    }

    public Task<bool> ManufacturerNameExistsAsync(string normalizedName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedName);

        return _dbContext.Manufacturers
            .AsNoTracking()
            .AnyAsync(manufacturer => manufacturer.NormalizedName == normalizedName, cancellationToken);
    }

    public Task<bool> ActiveAliasExistsAsync(string normalizedPhrase, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedPhrase);

        return _dbContext.ManufacturerAliases
            .AsNoTracking()
            .AnyAsync(
                manufacturerAlias =>
                    manufacturerAlias.NormalizedPhrase == normalizedPhrase &&
                    (manufacturerAlias.Status == ManufacturerAliasStatus.Pending ||
                     manufacturerAlias.Status == ManufacturerAliasStatus.Approved),
                cancellationToken);
    }

    public void Add(ManufacturerAlias manufacturerAlias)
    {
        ArgumentNullException.ThrowIfNull(manufacturerAlias);

        _dbContext.ManufacturerAliases.Add(manufacturerAlias);
    }
}