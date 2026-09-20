using ElectronicService.Core.Users;
using ElectronicService.Domain.Users;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Domain.Users.ValueObjects;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Users;

public sealed class UserRepository : IUserRepository
{
    private readonly ElectronicDbContext _dbContext;

    public UserRepository(ElectronicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .FirstOrDefaultAsync(
                user => user.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<User>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        if (ids.Count == 0)
        {
            return [];
        }

        return await _dbContext.Users
            .AsNoTracking()
            .Where(user => ids.Contains(user.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<User?> GetByEmailAsync(
        Email email,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .FirstOrDefaultAsync(
                user => user.Email == email,
                cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(
        Email email,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .AnyAsync(
                user => user.Email == email,
                cancellationToken);
    }

    public async Task<(IReadOnlyCollection<User> Items, int TotalCount)> GetPageAsync(
        string? search,
        UserType? type,
        UserStatus? status,
        bool includeSystemDeveloper,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<User> query;

        if (string.IsNullOrWhiteSpace(search))
        {
            query = _dbContext.Users.AsNoTracking();
        }
        else
        {
            var searchPattern = $"%{search.Trim()}%";

            query = _dbContext.Users
                .FromSqlInterpolated($"SELECT * FROM users WHERE display_name ILIKE {searchPattern} OR email ILIKE {searchPattern}")
                .AsNoTracking();
        }

        if (type.HasValue)
        {
            query = query.Where(user => user.Type == type.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(user => user.Status == status.Value);
        }

        if (!includeSystemDeveloper)
        {
            query = query.Where(
                user => user.Type != UserType.SystemDeveloper);
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await query
            .OrderByDescending(user => user.CreatedAtUtc)
            .ThenBy(user => user.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, totalCount);
    }

    public void Add(User user)
    {
        _dbContext.Users.Add(user);
    }
}
