using ElectronicService.Core.Catalog.ProductTypes.Abstractions;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class ProductTypeManagementRepository
    : IProductTypeManagementRepository
{
    private readonly ElectronicDbContext _dbContext;

    public ProductTypeManagementRepository(
        ElectronicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsByCodeAsync(
        string normalizedCode,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ProductTypes
            .AsNoTracking()
            .AnyAsync(
                productType => productType.Code == normalizedCode,
                cancellationToken);
    }

    public void Add(ProductType productType)
    {
        ArgumentNullException.ThrowIfNull(productType);

        _dbContext.ProductTypes.Add(productType);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
