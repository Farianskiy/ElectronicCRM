using ElectronicService.Core.Users;
using ElectronicService.Domain.Users;
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
