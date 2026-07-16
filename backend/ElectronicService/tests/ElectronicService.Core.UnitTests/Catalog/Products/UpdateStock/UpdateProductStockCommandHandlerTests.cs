using ElectronicService.Core.Catalog.Products.UpdateStock;
using ElectronicService.Core.UnitTests.TestDoubles;
using ElectronicService.TestCommon;

namespace ElectronicService.Core.UnitTests.Catalog.Products.UpdateStock;

public sealed class UpdateProductStockCommandHandlerTests
{
    // Проверяет, что handler отклоняет null вместо команды
    // до обращения к репозиторию товаров.
    [Fact]
    public async Task HandleThrowsArgumentNullExceptionWhenCommandIsNull()
    {
        // Arrange
        var repository = new FakeProductRepository();
        var handler = new UpdateProductStockCommandHandler(repository);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.Handle(null!, cancellationToken));

        // Assert
        Assert.Equal("command", exception.ParamName);
        Assert.Equal(0, repository.GetByIdCallsCount);
        Assert.Equal(0, repository.SaveChangesCallsCount);
    }

    // Проверяет, что пустой ProductId возвращает ошибку валидации,
    // не запускает поиск товара и не сохраняет изменения.
    [Fact]
    public async Task HandleReturnsInvalidErrorWhenProductIdIsEmpty()
    {
        // Arrange
        var repository = new FakeProductRepository();
        var handler = new UpdateProductStockCommandHandler(repository);

        var command = new UpdateProductStockCommand(
            Guid.Empty,
            10m);

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_invalid", result.Error.Code);
        Assert.Equal(0, repository.GetByIdCallsCount);
        Assert.Equal(0, repository.SaveChangesCallsCount);
    }

    // Проверяет, что отсутствующий товар возвращает ProductNotFound
    // и handler не вызывает SaveChangesAsync.
    [Fact]
    public async Task HandleReturnsNotFoundWhenProductDoesNotExist()
    {
        // Arrange
        var repository = new FakeProductRepository();
        var handler = new UpdateProductStockCommandHandler(repository);
        var productId = Guid.NewGuid();

        var command = new UpdateProductStockCommand(
            productId,
            10m);

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("catalog.product.not_found", result.Error.Code);
        Assert.Equal(productId, repository.LastGetByIdProductId);
        Assert.Equal(1, repository.GetByIdCallsCount);
        Assert.Equal(0, repository.SaveChangesCallsCount);
    }

    // Проверяет, что отрицательный остаток отклоняется,
    // существующее количество товара не меняется и не сохраняется.
    [Fact]
    public async Task HandleRejectsNegativeStockAndKeepsOldQuantity()
    {
        // Arrange
        var product = TestDataFactory.CreateProduct(stockQuantity: 25m);
        var repository = new FakeProductRepository();

        repository.AddExisting(product);

        var handler = new UpdateProductStockCommandHandler(repository);

        var command = new UpdateProductStockCommand(
            product.Id,
            -1m);

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_invalid", result.Error.Code);
        Assert.Equal(25m, product.StockQuantity.Value);
        Assert.True(product.IsAvailable);
        Assert.Equal(0, repository.SaveChangesCallsCount);
    }

    // Проверяет, что установка нулевого остатка делает товар недоступным
    // и сохраняет новое состояние один раз.
    [Fact]
    public async Task HandleSetsZeroStockAndMakesProductUnavailable()
    {
        // Arrange
        var product = TestDataFactory.CreateProduct(stockQuantity: 5m);
        var repository = new FakeProductRepository();

        repository.AddExisting(product);

        var handler = new UpdateProductStockCommandHandler(repository);

        var command = new UpdateProductStockCommand(
            product.Id,
            0m);

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0m, product.StockQuantity.Value);
        Assert.False(product.IsAvailable);
        Assert.NotNull(product.UpdatedAtUtc);
        Assert.Equal(1, repository.SaveChangesCallsCount);
    }

    // Проверяет, что положительный дробный остаток допустим,
    // делает товар доступным и сохраняет точное decimal-значение.
    [Fact]
    public async Task HandleSetsPositiveFractionalStockAndSavesChanges()
    {
        // Arrange
        var product = TestDataFactory.CreateProduct(stockQuantity: 0m);
        var repository = new FakeProductRepository();

        repository.AddExisting(product);

        var handler = new UpdateProductStockCommandHandler(repository);

        var command = new UpdateProductStockCommand(
            product.Id,
            12.5m);

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(12.5m, product.StockQuantity.Value);
        Assert.True(product.IsAvailable);
        Assert.Equal(1, repository.GetByIdCallsCount);
        Assert.Equal(1, repository.SaveChangesCallsCount);
    }

    // Проверяет, что CancellationToken xUnit без изменений передаётся
    // в поиск товара и сохранение репозитория.
    [Fact]
    public async Task HandlePassesCancellationTokenToRepository()
    {
        // Arrange
        var product = TestDataFactory.CreateProduct();
        var repository = new FakeProductRepository();

        repository.AddExisting(product);

        var handler = new UpdateProductStockCommandHandler(repository);
        var cancellationToken = TestContext.Current.CancellationToken;

        var command = new UpdateProductStockCommand(
            product.Id,
            30m);

        // Act
        var result = await handler.Handle(
            command,
            cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(
            cancellationToken,
            repository.LastGetByIdCancellationToken);
        Assert.Equal(
            cancellationToken,
            repository.LastSaveChangesCancellationToken);
    }
}
