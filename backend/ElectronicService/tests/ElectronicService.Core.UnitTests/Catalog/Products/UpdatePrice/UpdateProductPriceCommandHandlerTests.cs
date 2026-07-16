using ElectronicService.Core.Catalog.Products.UpdatePrice;
using ElectronicService.Core.UnitTests.TestDoubles;
using ElectronicService.TestCommon;

namespace ElectronicService.Core.UnitTests.Catalog.Products.UpdatePrice;

public sealed class UpdateProductPriceCommandHandlerTests
{
    // Проверяет, что handler не принимает null вместо команды
    // и завершает выполнение до обращения к репозиторию.
    [Fact]
    public async Task HandleThrowsArgumentNullExceptionWhenCommandIsNull()
    {
        // Arrange
        var repository = new FakeProductRepository();
        var handler = new UpdateProductPriceCommandHandler(repository);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.Handle(null!, cancellationToken));

        // Assert
        Assert.Equal("command", exception.ParamName);
        Assert.Equal(0, repository.GetByIdCallsCount);
        Assert.Equal(0, repository.SaveChangesCallsCount);
    }

    // Проверяет, что пустой ProductId отклоняется до поиска товара
    // и изменения не отправляются на сохранение.
    [Fact]
    public async Task HandleReturnsInvalidErrorWhenProductIdIsEmpty()
    {
        // Arrange
        var repository = new FakeProductRepository();
        var handler = new UpdateProductPriceCommandHandler(repository);

        var command = new UpdateProductPriceCommand(
            Guid.Empty,
            1_000m,
            "RUB");

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

    // Проверяет, что неизвестный ProductId возвращает ошибку ProductNotFound
    // и не вызывает сохранение.
    [Fact]
    public async Task HandleReturnsNotFoundWhenProductDoesNotExist()
    {
        // Arrange
        var repository = new FakeProductRepository();
        var handler = new UpdateProductPriceCommandHandler(repository);
        var productId = Guid.NewGuid();

        var command = new UpdateProductPriceCommand(
            productId,
            1_000m,
            "RUB");

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("catalog.product.not_found", result.Error.Code);
        Assert.Equal(1, repository.GetByIdCallsCount);
        Assert.Equal(productId, repository.LastGetByIdProductId);
        Assert.Equal(0, repository.SaveChangesCallsCount);
    }

    // Проверяет, что отрицательная цена не проходит доменную валидацию,
    // старая цена товара сохраняется, а SaveChangesAsync не вызывается.
    [Fact]
    public async Task HandleRejectsNegativePriceAndKeepsOldValue()
    {
        // Arrange
        var product = TestDataFactory.CreateProduct(price: 500m);
        var repository = new FakeProductRepository();

        repository.AddExisting(product);

        var handler = new UpdateProductPriceCommandHandler(repository);

        var command = new UpdateProductPriceCommand(
            product.Id,
            -0.01m,
            "RUB");

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_invalid", result.Error.Code);
        Assert.Equal(500m, product.Price.Amount);
        Assert.Equal("RUB", product.Price.Currency);
        Assert.Equal(0, repository.SaveChangesCallsCount);
    }

    // Проверяет, что пустой код валюты возвращает ошибку обязательного значения,
    // не изменяет товар и не сохраняет данные.
    [Fact]
    public async Task HandleRejectsBlankCurrencyAndKeepsOldPrice()
    {
        // Arrange
        var product = TestDataFactory.CreateProduct(price: 750m);
        var repository = new FakeProductRepository();

        repository.AddExisting(product);

        var handler = new UpdateProductPriceCommandHandler(repository);

        var command = new UpdateProductPriceCommand(
            product.Id,
            800m,
            " ");

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_required", result.Error.Code);
        Assert.Equal(750m, product.Price.Amount);
        Assert.Equal(0, repository.SaveChangesCallsCount);
    }

    // Проверяет, что код валюты неправильной длины отклоняется,
    // а существующая цена остаётся неизменной.
    [Fact]
    public async Task HandleRejectsCurrencyWithInvalidLength()
    {
        // Arrange
        var product = TestDataFactory.CreateProduct(price: 900m);
        var repository = new FakeProductRepository();

        repository.AddExisting(product);

        var handler = new UpdateProductPriceCommandHandler(repository);

        var command = new UpdateProductPriceCommand(
            product.Id,
            950m,
            "RUBL");

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_invalid", result.Error.Code);
        Assert.Equal(900m, product.Price.Amount);
        Assert.Equal("RUB", product.Price.Currency);
        Assert.Equal(0, repository.SaveChangesCallsCount);
    }

    // Проверяет полный успешный сценарий:
    // товар находится, цена меняется, валюта нормализуется и изменения сохраняются один раз.
    [Fact]
    public async Task HandleChangesPriceAndSavesChanges()
    {
        // Arrange
        var product = TestDataFactory.CreateProduct(price: 1_000m);
        var repository = new FakeProductRepository();

        repository.AddExisting(product);

        var handler = new UpdateProductPriceCommandHandler(repository);

        var command = new UpdateProductPriceCommand(
            product.Id,
            1_250.50m,
            " usd ");

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1_250.50m, product.Price.Amount);
        Assert.Equal("USD", product.Price.Currency);
        Assert.NotNull(product.UpdatedAtUtc);
        Assert.Equal(1, repository.GetByIdCallsCount);
        Assert.Equal(1, repository.SaveChangesCallsCount);
    }

    // Проверяет, что нулевая цена является допустимым значением,
    // заменяет старую цену и сохраняется в репозитории.
    [Fact]
    public async Task HandleAllowsZeroPrice()
    {
        // Arrange
        var product = TestDataFactory.CreateProduct(price: 100m);
        var repository = new FakeProductRepository();

        repository.AddExisting(product);

        var handler = new UpdateProductPriceCommandHandler(repository);

        var command = new UpdateProductPriceCommand(
            product.Id,
            0m,
            "RUB");

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0m, product.Price.Amount);
        Assert.Equal("RUB", product.Price.Currency);
        Assert.Equal(1, repository.SaveChangesCallsCount);
    }

    // Проверяет, что CancellationToken текущего теста передаётся
    // и в GetByIdAsync, и в SaveChangesAsync без подмены.
    [Fact]
    public async Task HandlePassesCancellationTokenToRepository()
    {
        // Arrange
        var product = TestDataFactory.CreateProduct();
        var repository = new FakeProductRepository();

        repository.AddExisting(product);

        var handler = new UpdateProductPriceCommandHandler(repository);
        var cancellationToken = TestContext.Current.CancellationToken;

        var command = new UpdateProductPriceCommand(
            product.Id,
            2_000m,
            "RUB");

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
