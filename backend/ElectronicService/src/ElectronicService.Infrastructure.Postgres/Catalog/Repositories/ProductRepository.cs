using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Domain.Catalog.Products;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly ElectronicDbContext _dbContext;

    public ProductRepository(ElectronicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Product?> GetByIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Products
            .FirstOrDefaultAsync(
                product => product.Id == productId,
                cancellationToken);
    }

    public Task<Product?> GetByIdWithDetailsAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Products
            .Include(product => product.Characteristics)
            .Include(product => product.Aliases)
            .FirstOrDefaultAsync(
                product => product.Id == productId,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<Product>> GetByIdsWithDetailsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(productIds);

        var ids = productIds
            .Where(productId => productId != Guid.Empty)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return [];
        }

        return await _dbContext.Products
            .Include(product => product.Characteristics)
            .Include(product => product.Aliases)
            .Where(product => ids.Contains(product.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> TrySaveChangesAsync(
    CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
