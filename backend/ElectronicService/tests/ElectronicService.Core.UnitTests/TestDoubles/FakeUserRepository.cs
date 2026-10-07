using ElectronicService.Core.Users;
using ElectronicService.Domain.Users;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Domain.Users.ValueObjects;

namespace ElectronicService.Core.UnitTests.TestDoubles;

/// <summary>
/// Простая реализация IUserRepository в памяти.
/// Она позволяет проверять взаимодействие handler с репозиторием
/// без PostgreSQL, EF Core и mocking-библиотеки.
/// </summary>
internal sealed class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = [];

    public IReadOnlyCollection<User> Users => _users.AsReadOnly();

    public int GetByIdCallsCount { get; private set; }

    public int GetByEmailCallsCount { get; private set; }

    public int ExistsByEmailCallsCount { get; private set; }

    public int AddCallsCount { get; private set; }

    public Guid? LastRequestedUserId { get; private set; }

    public Email? LastRequestedEmail { get; private set; }

    public Email? LastCheckedEmail { get; private set; }

    public CancellationToken LastGetByIdCancellationToken { get; private set; }

    public CancellationToken LastGetByEmailCancellationToken { get; private set; }

    public CancellationToken LastExistsByEmailCancellationToken { get; private set; }

    public User? AddedUser { get; private set; }

    public Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        GetByIdCallsCount++;
        LastRequestedUserId = id;
        LastGetByIdCancellationToken = cancellationToken;

        var user = _users.FirstOrDefault(candidate => candidate.Id == id);

        return Task.FromResult(user);
    }

    public Task<IReadOnlyCollection<User>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        var requestedIds = ids.ToHashSet();
        IReadOnlyCollection<User> users = _users
            .Where(user => requestedIds.Contains(user.Id))
            .ToArray();

        return Task.FromResult(users);
    }

    public Task<User?> GetByEmailAsync(
        Email email,
        CancellationToken cancellationToken = default)
    {
        GetByEmailCallsCount++;
        LastRequestedEmail = email;
        LastGetByEmailCancellationToken = cancellationToken;

        var user = _users.FirstOrDefault(candidate =>
            candidate.Email is not null &&
            candidate.Email.Equals(email));

        return Task.FromResult(user);
    }

    public Task<bool> ExistsByEmailAsync(
        Email email,
        CancellationToken cancellationToken = default)
    {
        ExistsByEmailCallsCount++;
        LastCheckedEmail = email;
        LastExistsByEmailCancellationToken = cancellationToken;

        var exists = _users.Any(candidate =>
            candidate.Email is not null &&
            candidate.Email.Equals(email));

        return Task.FromResult(exists);
    }

    public Task<bool> HasPermissionAsync(
        User user,
        UserPermissionCode permission,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var hasPermission =
            ElectronicService.Core.Users.Access.UserPermissionResolver
                .HasPermission(user, permission);

        return Task.FromResult(hasPermission);
    }

    public Task<(IReadOnlyCollection<User> Items, int TotalCount)> GetPageAsync(
        string? search,
        UserType? type,
        UserStatus? status,
        bool includeSystemDeveloper,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<User> query = _users;

        if (!includeSystemDeveloper)
        {
            query = query.Where(user => !user.IsSystemDeveloper);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(user =>
                user.DisplayName.Value.Contains(
                    search,
                    StringComparison.OrdinalIgnoreCase)
                || user.Email?.Value.Contains(
                    search,
                    StringComparison.OrdinalIgnoreCase) == true);
        }

        if (type.HasValue)
        {
            query = query.Where(user => user.Type == type.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(user => user.Status == status.Value);
        }

        var filtered = query.ToArray();
        var items = filtered
            .Skip((Math.Max(page, 1) - 1) * Math.Max(pageSize, 1))
            .Take(Math.Max(pageSize, 1))
            .ToArray();

        return Task.FromResult<(
            IReadOnlyCollection<User> Items,
            int TotalCount)>((items, filtered.Length));
    }

    public void Add(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        AddCallsCount++;
        AddedUser = user;
        _users.Add(user);
    }

    public void Seed(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        _users.Add(user);
    }
}
