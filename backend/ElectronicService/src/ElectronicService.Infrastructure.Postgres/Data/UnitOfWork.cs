using ElectronicService.Core.Abstractions.Data;

namespace ElectronicService.Infrastructure.Postgres.Data;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ElectronicDbContext _dbContext;

    public UnitOfWork(ElectronicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}