using ElectronicService.Core.Users.Access;
using ElectronicService.Domain.Users;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Users;

public sealed class UserPermissionOverrideRepository : IUserPermissionOverrideRepository
{
    private readonly ElectronicDbContext _dbContext;

    public UserPermissionOverrideRepository(ElectronicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<UserPermissionOverride>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserPermissionOverrides
            .AsNoTracking()
            .Where(value => value.UserId == userId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task ReplaceAsync(
        Guid userId,
        IReadOnlyCollection<UserPermissionOverride> values,
        CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.UserPermissionOverrides
            .Where(value => value.UserId == userId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        _dbContext.UserPermissionOverrides.RemoveRange(existing);

        await _dbContext.UserPermissionOverrides
            .AddRangeAsync(values, cancellationToken)
            .ConfigureAwait(false);
    }
}