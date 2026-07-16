using ElectronicService.Core.UnitTests.TestDoubles;
using ElectronicService.Core.Users.CreateTechnicalUser;
using ElectronicService.TestCommon;

namespace ElectronicService.Core.UnitTests.Users.CreateTechnicalUser;

public sealed class CreateTechnicalUserCommandHandlerTests
{
    // Проверяет полный успешный сценарий создания технического пользователя и сохранение хеша пароля.
    [Fact]
    public async Task HandleCreatesTechnicalUserAndSavesChanges()
    {
        // Arrange
        var repository = new FakeUserRepository();
        var unitOfWork = new FakeUnitOfWork();
        var passwordHasher = new FakePasswordHasher
        {
            HashResult = "technical-password-hash"
        };
        var handler = new CreateTechnicalUserCommandHandler(
            repository,
            unitOfWork,
            passwordHasher);
        var command = new CreateTechnicalUserCommand(
            "Технический пользователь",
            "technical@example.com",
            "plain-password");

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.AddedUser);
        Assert.Equal(repository.AddedUser.Id, result.Value);
        Assert.True(repository.AddedUser.IsTechnical);
        Assert.Equal("TECHNICAL@EXAMPLE.COM", repository.AddedUser.Email?.Value);
        Assert.Equal("technical-password-hash", repository.AddedUser.PasswordHash);
        Assert.Equal(1, repository.ExistsByEmailCallsCount);
        Assert.Equal(1, repository.AddCallsCount);
        Assert.Equal(1, unitOfWork.SaveChangesCallsCount);
        Assert.Equal(1, passwordHasher.HashCallsCount);
    }

    // Проверяет, что технический пользователь не создаётся с некорректным email.
    [Fact]
    public async Task HandleDoesNotSaveTechnicalUserWhenEmailIsInvalid()
    {
        // Arrange
        var repository = new FakeUserRepository();
        var unitOfWork = new FakeUnitOfWork();
        var passwordHasher = new FakePasswordHasher();
        var handler = new CreateTechnicalUserCommandHandler(
            repository,
            unitOfWork,
            passwordHasher);
        var command = new CreateTechnicalUserCommand(
            "Технический пользователь",
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

    // Проверяет, что невалидное имя технического пользователя не приводит к записи в репозиторий.
    [Fact]
    public async Task HandleDoesNotSaveTechnicalUserWhenDisplayNameIsInvalid()
    {
        // Arrange
        var repository = new FakeUserRepository();
        var unitOfWork = new FakeUnitOfWork();
        var passwordHasher = new FakePasswordHasher();
        var handler = new CreateTechnicalUserCommandHandler(
            repository,
            unitOfWork,
            passwordHasher);
        var command = new CreateTechnicalUserCommand(
            " ",
            "technical@example.com",
            "plain-password");

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_required", result.Error.Code);
        Assert.Equal(0, repository.ExistsByEmailCallsCount);
        Assert.Equal(0, repository.AddCallsCount);
        Assert.Equal(0, unitOfWork.SaveChangesCallsCount);
    }

    // Проверяет запрет создания второго пользователя с тем же email.
    [Fact]
    public async Task HandleReturnsEmailAlreadyTakenWhenEmailExists()
    {
        // Arrange
        var repository = new FakeUserRepository();
        repository.Seed(
            TestDataFactory.CreateTechnicalUser(
                email: "technical@example.com"));

        var unitOfWork = new FakeUnitOfWork();
        var passwordHasher = new FakePasswordHasher();
        var handler = new CreateTechnicalUserCommandHandler(
            repository,
            unitOfWork,
            passwordHasher);
        var command = new CreateTechnicalUserCommand(
            "Другой технический пользователь",
            "technical@example.com",
            "plain-password");

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("user.email_already_taken", result.Error.Code);
        Assert.Equal(0, repository.AddCallsCount);
        Assert.Equal(0, unitOfWork.SaveChangesCallsCount);
        Assert.Single(repository.Users);
    }

    // Проверяет передачу CancellationToken во все асинхронные зависимости успешного сценария.
    [Fact]
    public async Task HandlePassesCancellationTokenToDependencies()
    {
        // Arrange
        var repository = new FakeUserRepository();
        var unitOfWork = new FakeUnitOfWork();
        var passwordHasher = new FakePasswordHasher();
        var handler = new CreateTechnicalUserCommandHandler(
            repository,
            unitOfWork,
            passwordHasher);
        var command = new CreateTechnicalUserCommand(
            "Технический пользователь",
            "technical@example.com",
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
