using ElectronicService.Core.UnitTests.TestDoubles;
using ElectronicService.Core.Users.MakeUserTechnical;
using ElectronicService.TestCommon;

namespace ElectronicService.Core.UnitTests.Users.MakeUserTechnical;

public sealed class MakeUserTechnicalCommandHandlerTests
{
    // Проверяет успешное повышение обычного пользователя до технического и сохранение изменений.
    [Fact]
    public async Task HandleMakesRegularUserTechnicalAndSavesChanges()
    {
        // Arrange
        var user = TestDataFactory.CreateRegularUser(email: null);
        var repository = new FakeUserRepository();
        repository.Seed(user);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new MakeUserTechnicalCommandHandler(
            repository,
            unitOfWork);
        var command = new MakeUserTechnicalCommand(
            user.Id,
            "technical@example.com");

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(user.IsTechnical);
        Assert.Equal("TECHNICAL@EXAMPLE.COM", user.Email?.Value);
        Assert.Equal(1, repository.GetByIdCallsCount);
        Assert.Equal(1, repository.ExistsByEmailCallsCount);
        Assert.Equal(1, unitOfWork.SaveChangesCallsCount);
    }

    // Проверяет, что отсутствующий пользователь возвращает NotFound и не вызывает сохранение.
    [Fact]
    public async Task HandleReturnsNotFoundWhenUserDoesNotExist()
    {
        // Arrange
        var repository = new FakeUserRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new MakeUserTechnicalCommandHandler(
            repository,
            unitOfWork);
        var userId = Guid.NewGuid();
        var command = new MakeUserTechnicalCommand(
            userId,
            "technical@example.com");

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("user.not_found", result.Error.Code);
        Assert.Equal(userId, repository.LastRequestedUserId);
        Assert.Equal(0, repository.ExistsByEmailCallsCount);
        Assert.Equal(0, unitOfWork.SaveChangesCallsCount);
    }

    // Проверяет, что некорректный email отклоняется до проверки уникальности и изменения пользователя.
    [Fact]
    public async Task HandleReturnsValidationErrorWhenEmailIsInvalid()
    {
        // Arrange
        var user = TestDataFactory.CreateRegularUser(email: null);
        var repository = new FakeUserRepository();
        repository.Seed(user);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new MakeUserTechnicalCommandHandler(
            repository,
            unitOfWork);
        var command = new MakeUserTechnicalCommand(
            user.Id,
            "invalid-email");

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_invalid", result.Error.Code);
        Assert.True(user.IsRegular);
        Assert.Equal(0, repository.ExistsByEmailCallsCount);
        Assert.Equal(0, unitOfWork.SaveChangesCallsCount);
    }

    // Проверяет запрет повышения, если указанный email принадлежит другому пользователю.
    [Fact]
    public async Task HandleReturnsEmailAlreadyTakenWhenEmailBelongsToAnotherUser()
    {
        // Arrange
        var currentUser = TestDataFactory.CreateRegularUser(
            email: "current@example.com");
        var otherUser = TestDataFactory.CreateTechnicalUser(
            email: "technical@example.com");
        var repository = new FakeUserRepository();
        repository.Seed(currentUser);
        repository.Seed(otherUser);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new MakeUserTechnicalCommandHandler(
            repository,
            unitOfWork);
        var command = new MakeUserTechnicalCommand(
            currentUser.Id,
            "technical@example.com");

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("user.email_already_taken", result.Error.Code);
        Assert.True(currentUser.IsRegular);
        Assert.Equal("CURRENT@EXAMPLE.COM", currentUser.Email?.Value);
        Assert.Equal(0, unitOfWork.SaveChangesCallsCount);
    }

    // Проверяет, что пользователь может сохранить собственный email при повышении роли.
    [Fact]
    public async Task HandleAllowsCurrentUserToKeepOwnEmail()
    {
        // Arrange
        var user = TestDataFactory.CreateRegularUser(
            email: "current@example.com");
        var repository = new FakeUserRepository();
        repository.Seed(user);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new MakeUserTechnicalCommandHandler(
            repository,
            unitOfWork);
        var command = new MakeUserTechnicalCommand(
            user.Id,
            "current@example.com");

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(user.IsTechnical);
        Assert.Equal("CURRENT@EXAMPLE.COM", user.Email?.Value);
        Assert.Equal(1, unitOfWork.SaveChangesCallsCount);
    }

    // Проверяет, что повторное повышение уже технического пользователя возвращает доменную ошибку и не сохраняется.
    [Fact]
    public async Task HandleDoesNotSaveWhenUserIsAlreadyTechnical()
    {
        // Arrange
        var user = TestDataFactory.CreateTechnicalUser();
        var repository = new FakeUserRepository();
        repository.Seed(user);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new MakeUserTechnicalCommandHandler(
            repository,
            unitOfWork);
        var command = new MakeUserTechnicalCommand(
            user.Id,
            user.Email!.Value);

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("user.already_technical", result.Error.Code);
        Assert.Equal(0, unitOfWork.SaveChangesCallsCount);
    }

    // Проверяет, что заблокированного пользователя нельзя повысить до технического.
    [Fact]
    public async Task HandleDoesNotSaveWhenUserIsBlocked()
    {
        // Arrange
        var user = TestDataFactory.CreateRegularUser(email: null);
        user.Block();
        var repository = new FakeUserRepository();
        repository.Seed(user);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new MakeUserTechnicalCommandHandler(
            repository,
            unitOfWork);
        var command = new MakeUserTechnicalCommand(
            user.Id,
            "technical@example.com");

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("user.blocked_user_cannot_be_changed", result.Error.Code);
        Assert.True(user.IsRegular);
        Assert.Equal(0, unitOfWork.SaveChangesCallsCount);
    }

    // Проверяет передачу CancellationToken в поиск пользователя, проверку email и сохранение изменений.
    [Fact]
    public async Task HandlePassesCancellationTokenToDependencies()
    {
        // Arrange
        var user = TestDataFactory.CreateRegularUser(email: null);
        var repository = new FakeUserRepository();
        repository.Seed(user);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new MakeUserTechnicalCommandHandler(
            repository,
            unitOfWork);
        var command = new MakeUserTechnicalCommand(
            user.Id,
            "technical@example.com");
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await handler.Handle(command, cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(
            cancellationToken,
            repository.LastGetByIdCancellationToken);
        Assert.Equal(
            cancellationToken,
            repository.LastExistsByEmailCancellationToken);
        Assert.Equal(
            cancellationToken,
            unitOfWork.LastCancellationToken);
    }
}
