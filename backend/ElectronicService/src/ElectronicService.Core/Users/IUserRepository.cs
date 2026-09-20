using ElectronicService.Domain.Users;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Domain.Users.ValueObjects;

namespace ElectronicService.Core.Users;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<User>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(
        Email email,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(
        Email email,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyCollection<User> Items, int TotalCount)> GetPageAsync(
        string? search,
        UserType? type,
        UserStatus? status,
        bool includeSystemDeveloper,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    void Add(User user);
}
