using ElectronicService.Domain.Users.ValueObjects;
using ElectronicService.Infrastructure.IntegrationTests.Fixtures;
using ElectronicService.Infrastructure.Postgres.Users;
using ElectronicService.TestCommon;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.IntegrationTests.Users;

[Collection(PostgreSqlIntegrationDefinition.Name)]
public sealed class UserRepositoryTests : PostgreSqlIntegrationTest
{
    public UserRepositoryTests(PostgreSqlFixture fixture)
        : base(fixture)
    {
    }

    // Проверяет настоящий UserRepository.Add и GetByIdAsync:
    // User и его Value Objects сохраняются и восстанавливаются из PostgreSQL.
    [Fact]
    public async Task AddAndGetByIdAsyncPersistsUser()
    {
        // Arrange
        var suffix = Guid.NewGuid().ToString("N");

        var user = TestDataFactory.CreateTechnicalUser(
            displayName: $"Технический пользователь {suffix}",
            email: $"technical-{suffix}@example.com",
            passwordHash: $"hash-{suffix}");

        var repository = new UserRepository(DbContext);

        repository.Add(user);

        await DbContext.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        DbContext.ChangeTracker.Clear();

        // Act
        var loadedUser = await repository.GetByIdAsync(
            user.Id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(loadedUser);
        Assert.Equal(user.Id, loadedUser.Id);
        Assert.Equal(
            user.DisplayName.Value,
            loadedUser.DisplayName.Value);
        Assert.Equal(user.Email?.Value, loadedUser.Email?.Value);
        Assert.Equal(user.PasswordHash, loadedUser.PasswordHash);
        Assert.True(loadedUser.IsTechnical);
        Assert.True(loadedUser.IsActive);
    }

    // Проверяет GetByEmailAsync с настоящим EF mapping Email:
    // входной email нормализуется Value Object и находит сохранённого пользователя.
    [Fact]
    public async Task GetByEmailAsyncFindsUserByNormalizedEmail()
    {
        // Arrange
        var suffix = Guid.NewGuid().ToString("N");

        var rawEmail =
            $"mixed-case-{suffix}@example.com";

        var user = TestDataFactory.CreateTechnicalUser(
            email: rawEmail);

        DbContext.Users.Add(user);

        await DbContext.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        DbContext.ChangeTracker.Clear();

        var repository = new UserRepository(DbContext);

        var emailResult = Email.Create(
            $"  MIXED-CASE-{suffix}@EXAMPLE.COM  ");

        Assert.True(emailResult.IsSuccess);

        // Act
        var loadedUser = await repository.GetByEmailAsync(
            emailResult.Value,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(loadedUser);
        Assert.Equal(user.Id, loadedUser.Id);
        Assert.Equal(
            emailResult.Value.Value,
            loadedUser.Email?.Value);
    }

    // Проверяет ExistsByEmailAsync на реальной PostgreSQL:
    // существующий нормализованный email возвращает true,
    // неизвестный email возвращает false.
    [Fact]
    public async Task ExistsByEmailAsyncReturnsExpectedValues()
    {
        // Arrange
        var suffix = Guid.NewGuid().ToString("N");

        var user = TestDataFactory.CreateTechnicalUser(
            email: $"exists-{suffix}@example.com");

        DbContext.Users.Add(user);

        await DbContext.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        DbContext.ChangeTracker.Clear();

        var repository = new UserRepository(DbContext);

        var existingEmail = Email.Create(
            $"exists-{suffix}@example.com");

        var missingEmail = Email.Create(
            $"missing-{suffix}@example.com");

        Assert.True(existingEmail.IsSuccess);
        Assert.True(missingEmail.IsSuccess);

        // Act
        var exists = await repository.ExistsByEmailAsync(
            existingEmail.Value,
            TestContext.Current.CancellationToken);

        var missing = await repository.ExistsByEmailAsync(
            missingEmail.Value,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(exists);
        Assert.False(missing);
    }
}
