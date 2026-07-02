using ElectronicService.Core.Users;
using ElectronicService.Domain.Users;
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
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(
        Email email,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(
        Email email,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .AnyAsync(user => user.Email == email, cancellationToken);
    }

    public void Add(User user)
    {
        _dbContext.Users.Add(user);
    }
}