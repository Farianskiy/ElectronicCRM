using ElectronicService.Core.UnitTests.TestDoubles;
using ElectronicService.Core.Users.MakeUserRegular;
using ElectronicService.TestCommon;

namespace ElectronicService.Core.UnitTests.Users.MakeUserRegular;

public sealed class MakeUserRegularCommandHandlerTests
{
    // Проверяет успешное понижение технического пользователя до обычного и сохранение изменений.
    [Fact]
    public async Task HandleMakesTechnicalUserRegularAndSavesChanges()
    {
        // Arrange
        var user = TestDataFactory.CreateTechnicalUser();
        var repository = new FakeUserRepository();
        repository.Seed(user);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new MakeUserRegularCommandHandler(
            repository,
            unitOfWork);
        var command = new MakeUserRegularCommand(user.Id);

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(user.IsRegular);
        Assert.False(user.IsTechnical);
        Assert.Equal(1, repository.GetByIdCallsCount);
        Assert.Equal(1, unitOfWork.SaveChangesCallsCount);
    }

    // Проверяет, что отсутствующий пользователь возвращает NotFound без сохранения изменений.
    [Fact]
    public async Task HandleReturnsNotFoundWhenUserDoesNotExist()
    {
        // Arrange
        var repository = new FakeUserRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new MakeUserRegularCommandHandler(
            repository,
            unitOfWork);
        var userId = Guid.NewGuid();
        var command = new MakeUserRegularCommand(userId);

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("user.not_found", result.Error.Code);
        Assert.Equal(userId, repository.LastRequestedUserId);
        Assert.Equal(0, unitOfWork.SaveChangesCallsCount);
    }

    // Проверяет, что уже обычного пользователя нельзя повторно сделать обычным.
    [Fact]
    public async Task HandleDoesNotSaveWhenUserIsAlreadyRegular()
    {
        // Arrange
        var user = TestDataFactory.CreateRegularUser();
        var repository = new FakeUserRepository();
        repository.Seed(user);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new MakeUserRegularCommandHandler(
            repository,
            unitOfWork);
        var command = new MakeUserRegularCommand(user.Id);

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("user.already_regular", result.Error.Code);
        Assert.Equal(0, unitOfWork.SaveChangesCallsCount);
    }

    // Проверяет запрет изменения роли заблокированного пользователя.
    [Fact]
    public async Task HandleDoesNotSaveWhenUserIsBlocked()
    {
        // Arrange
        var user = TestDataFactory.CreateTechnicalUser();
        user.Block();
        var repository = new FakeUserRepository();
        repository.Seed(user);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new MakeUserRegularCommandHandler(
            repository,
            unitOfWork);
        var command = new MakeUserRegularCommand(user.Id);

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("user.blocked_user_cannot_be_changed", result.Error.Code);
        Assert.True(user.IsTechnical);
        Assert.Equal(0, unitOfWork.SaveChangesCallsCount);
    }

    // Проверяет передачу CancellationToken в репозиторий и UnitOfWork.
    [Fact]
    public async Task HandlePassesCancellationTokenToDependencies()
    {
        // Arrange
        var user = TestDataFactory.CreateTechnicalUser();
        var repository = new FakeUserRepository();
        repository.Seed(user);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new MakeUserRegularCommandHandler(
            repository,
            unitOfWork);
        var command = new MakeUserRegularCommand(user.Id);
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
            unitOfWork.LastCancellationToken);
    }
}
