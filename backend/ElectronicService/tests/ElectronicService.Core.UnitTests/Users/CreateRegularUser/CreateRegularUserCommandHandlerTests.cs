using ElectronicService.Core.UnitTests.TestDoubles;
using ElectronicService.Core.Users.CreateRegularUser;
using ElectronicService.TestCommon;

namespace ElectronicService.Core.UnitTests.Users.CreateRegularUser;

public sealed class CreateRegularUserCommandHandlerTests
{
    // Проверяет полный успешный сценарий: пароль хешируется, пользователь добавляется, изменения сохраняются, handler возвращает Id.
    [Fact]
    public async Task HandleCreatesRegularUserAndSavesChanges()
    {
        // Arrange
        var repository = new FakeUserRepository();
        var unitOfWork = new FakeUnitOfWork();
        var passwordHasher = new FakePasswordHasher
        {
            HashResult = "calculated-password-hash"
        };
        var handler = new CreateRegularUserCommandHandler(
            repository,
            unitOfWork,
            passwordHasher);
        var command = new CreateRegularUserCommand(
            "Fer",
            "fer@example.com",
            "plain-password");

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.AddedUser);
        Assert.Equal(repository.AddedUser.Id, result.Value);
        Assert.True(repository.AddedUser.IsRegular);
        Assert.Equal("Fer", repository.AddedUser.DisplayName.Value);
        Assert.Equal("FER@EXAMPLE.COM", repository.AddedUser.Email?.Value);
        Assert.Equal("calculated-password-hash", repository.AddedUser.PasswordHash);
        Assert.Equal(1, repository.ExistsByEmailCallsCount);
        Assert.Equal(1, repository.AddCallsCount);
        Assert.Equal(1, unitOfWork.SaveChangesCallsCount);
        Assert.Equal(1, passwordHasher.HashCallsCount);
        Assert.Equal("plain-password", passwordHasher.LastPasswordToHash);
    }

    // Проверяет, что обычного пользователя разрешено создать без email и проверка уникальности email при этом не выполняется.
    [Fact]
    public async Task HandleCreatesRegularUserWithoutEmail()
    {
        // Arrange
        var repository = new FakeUserRepository();
        var unitOfWork = new FakeUnitOfWork();
        var passwordHasher = new FakePasswordHasher();
        var handler = new CreateRegularUserCommandHandler(
            repository,
            unitOfWork,
            passwordHasher);
        var command = new CreateRegularUserCommand(
            "Fer",
            null,
            "plain-password");

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.AddedUser);
        Assert.Null(repository.AddedUser.Email);
        Assert.Equal(0, repository.ExistsByEmailCallsCount);
        Assert.Equal(1, repository.AddCallsCount);
        Assert.Equal(1, unitOfWork.SaveChangesCallsCount);
    }

    // Проверяет, что невалидное отображаемое имя возвращает доменную ошибку и пользователь не сохраняется.
    [Fact]
    public async Task HandleDoesNotSaveUserWhenDisplayNameIsInvalid()
    {
        // Arrange
        var repository = new FakeUserRepository();
        var unitOfWork = new FakeUnitOfWork();
        var passwordHasher = new FakePasswordHasher();
        var handler = new CreateRegularUserCommandHandler(
            repository,
            unitOfWork,
            passwordHasher);
        var command = new CreateRegularUserCommand(
            " ",
            "fer@example.com",
            "plain-password");

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_required", result.Error.Code);
        Assert.Equal(0, repository.ExistsByEmailCallsCount);
        Assert.Equal(0, repository.AddCallsCount);
        Assert.Equal(0, unitOfWork.SaveChangesCallsCount);
        Assert.Equal(1, passwordHasher.HashCallsCount);
    }

    // Проверяет, что невалидный email останавливает use case до обращения к репозиторию и сохранения.
    [Fact]
    public async Task HandleDoesNotSaveUserWhenEmailIsInvalid()
    {
        // Arrange
        var repository = new FakeUserRepository();
        var unitOfWork = new FakeUnitOfWork();
        var passwordHasher = new FakePasswordHasher();
        var handler = new CreateRegularUserCommandHandler(
            repository,
            unitOfWork,
            passwordHasher);
        var command = new CreateRegularUserCommand(
            "Fer",
            "invalid-email",
            "plain-password");

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_invalid", result.Error.Code);
        Assert.Equal(0, repository.ExistsByEmailCallsCount);
        Assert.Equal(0, repository.AddCallsCount);
        Assert.Equal(0, unitOfWork.SaveChangesCallsCount);
    }

    // Проверяет, что пользователь с уже занятым email не добавляется и UnitOfWork не вызывается.
    [Fact]
    public async Task HandleReturnsEmailAlreadyTakenWhenEmailExists()
    {
        // Arrange
        var repository = new FakeUserRepository();
        repository.Seed(
            TestDataFactory.CreateRegularUser(
                email: "fer@example.com"));

        var unitOfWork = new FakeUnitOfWork();
        var passwordHasher = new FakePasswordHasher();
        var handler = new CreateRegularUserCommandHandler(
            repository,
            unitOfWork,
            passwordHasher);
        var command = new CreateRegularUserCommand(
            "Другой пользователь",
            "fer@example.com",
            "plain-password");

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("user.email_already_taken", result.Error.Code);
        Assert.Equal(1, repository.ExistsByEmailCallsCount);
        Assert.Equal(0, repository.AddCallsCount);
        Assert.Equal(0, unitOfWork.SaveChangesCallsCount);
        Assert.Single(repository.Users);
    }

    // Проверяет передачу CancellationToken из handler в репозиторий и UnitOfWork.
    [Fact]
    public async Task HandlePassesCancellationTokenToDependencies()
    {
        // Arrange
        var repository = new FakeUserRepository();
        var unitOfWork = new FakeUnitOfWork();
        var passwordHasher = new FakePasswordHasher();
        var handler = new CreateRegularUserCommandHandler(
            repository,
            unitOfWork,
            passwordHasher);
        var command = new CreateRegularUserCommand(
            "Fer",
            "fer@example.com",
            "plain-password");
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await handler.Handle(command, cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(
            cancellationToken,
            repository.LastExistsByEmailCancellationToken);
        Assert.Equal(
            cancellationToken,
            unitOfWork.LastCancellationToken);
    }
}
