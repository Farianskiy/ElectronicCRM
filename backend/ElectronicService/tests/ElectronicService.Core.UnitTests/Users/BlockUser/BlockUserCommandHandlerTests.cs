using ElectronicService.Core.UnitTests.TestDoubles;
using ElectronicService.Core.Users.BlockUser;
using ElectronicService.TestCommon;

namespace ElectronicService.Core.UnitTests.Users.BlockUser;

public sealed class BlockUserCommandHandlerTests
{
    // Проверяет успешную блокировку пользователя и сохранение изменённого состояния.
    [Fact]
    public async Task HandleBlocksUserAndSavesChanges()
    {
        // Arrange
        var user = TestDataFactory.CreateRegularUser();
        var repository = new FakeUserRepository();
        repository.Seed(user);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new BlockUserCommandHandler(
            repository,
            unitOfWork);
        var command = new BlockUserCommand(user.Id);

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(user.IsBlocked);
        Assert.False(user.IsActive);
        Assert.Equal(1, repository.GetByIdCallsCount);
        Assert.Equal(1, unitOfWork.SaveChangesCallsCount);
    }

    // Проверяет, что попытка заблокировать отсутствующего пользователя возвращает NotFound.
    [Fact]
    public async Task HandleReturnsNotFoundWhenUserDoesNotExist()
    {
        // Arrange
        var repository = new FakeUserRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new BlockUserCommandHandler(
            repository,
            unitOfWork);
        var userId = Guid.NewGuid();
        var command = new BlockUserCommand(userId);

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("user.not_found", result.Error.Code);
        Assert.Equal(userId, repository.LastRequestedUserId);
        Assert.Equal(0, unitOfWork.SaveChangesCallsCount);
    }

    // Проверяет, что повторная блокировка возвращает доменную ошибку и не вызывает сохранение.
    [Fact]
    public async Task HandleDoesNotSaveWhenUserIsAlreadyBlocked()
    {
        // Arrange
        var user = TestDataFactory.CreateRegularUser();
        user.Block();
        var repository = new FakeUserRepository();
        repository.Seed(user);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new BlockUserCommandHandler(
            repository,
            unitOfWork);
        var command = new BlockUserCommand(user.Id);

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("user.already_blocked", result.Error.Code);
        Assert.Equal(0, unitOfWork.SaveChangesCallsCount);
    }

    // Проверяет передачу CancellationToken в поиск пользователя и сохранение изменений.
    [Fact]
    public async Task HandlePassesCancellationTokenToDependencies()
    {
        // Arrange
        var user = TestDataFactory.CreateRegularUser();
        var repository = new FakeUserRepository();
        repository.Seed(user);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new BlockUserCommandHandler(
            repository,
            unitOfWork);
        var command = new BlockUserCommand(user.Id);
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
