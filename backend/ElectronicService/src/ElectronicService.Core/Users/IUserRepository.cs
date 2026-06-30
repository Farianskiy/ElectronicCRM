using ElectronicService.Domain.Users;
using ElectronicService.Domain.Users.ValueObjects;

namespace ElectronicService.Core.Users;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(
        Email email,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(
        Email email,
        CancellationToken cancellationToken = default);

    void Add(User user);
}