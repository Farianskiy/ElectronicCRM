using ElectronicService.Core.Catalog.Products.AddAlias;
using ElectronicService.Core.UnitTests.TestDoubles;
using ElectronicService.TestCommon;

namespace ElectronicService.Core.UnitTests.Catalog.Products.AddAlias;

public sealed class AddProductAliasCommandHandlerTests
{
    // Проверяет, что handler отклоняет null вместо команды
    // до обращения к репозиторию товаров.
    [Fact]
    public async Task HandleThrowsArgumentNullExceptionWhenCommandIsNull()
    {
        // Arrange
        var repository = new FakeProductRepository();
        var handler = new AddProductAliasCommandHandler(repository);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.Handle(null!, cancellationToken));

        // Assert
        Assert.Equal("command", exception.ParamName);
        Assert.Equal(0, repository.GetByIdWithDetailsCallsCount);
        Assert.Equal(0, repository.SaveChangesCallsCount);
    }

    // Проверяет, что пустой ProductId возвращает ошибку валидации,
    // не запускает поиск товара и не сохраняет изменения.
    [Fact]
    public async Task HandleReturnsInvalidErrorWhenProductIdIsEmpty()
    {
        // Arrange
        var repository = new FakeProductRepository();
        var handler = new AddProductAliasCommandHandler(repository);

        var command = new AddProductAliasCommand(
            Guid.Empty,
            "Альтернативное название");

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_invalid", result.Error.Code);
        Assert.Equal(0, repository.GetByIdWithDetailsCallsCount);
        Assert.Equal(0, repository.SaveChangesCallsCount);
    }

    // Проверяет, что пустой или состоящий только из пробелов алиас
    // отклоняется до загрузки агрегата Product.
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public async Task HandleReturnsInvalidErrorWhenAliasIsBlank(
        string alias)
    {
        // Arrange
        var repository = new FakeProductRepository();
        var handler = new AddProductAliasCommandHandler(repository);

        var command = new AddProductAliasCommand(
            Guid.NewGuid(),
            alias);

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_invalid", result.Error.Code);
        Assert.Equal(0, repository.GetByIdWithDetailsCallsCount);
        Assert.Equal(0, repository.SaveChangesCallsCount);
    }

    // Проверяет, что неизвестный ProductId возвращает ProductNotFound
    // и не вызывает сохранение.
    [Fact]
    public async Task HandleReturnsNotFoundWhenProductDoesNotExist()
    {
        // Arrange
        var repository = new FakeProductRepository();
        var handler = new AddProductAliasCommandHandler(repository);
        var productId = Guid.NewGuid();

        var command = new AddProductAliasCommand(
            productId,
            "Альтернативное название");

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("catalog.product.not_found", result.Error.Code);
        Assert.Equal(1, repository.GetByIdWithDetailsCallsCount);
        Assert.Equal(
            productId,
            repository.LastGetByIdWithDetailsProductId);
        Assert.Equal(0, repository.SaveChangesCallsCount);
    }

    // Проверяет полный успешный сценарий:
    // алиас обрезается, нормализуется, добавляется к товару и сохраняется один раз.
    [Fact]
    public async Task HandleAddsNormalizedAliasAndSavesChanges()
    {
        // Arrange
        var product = TestDataFactory.CreateProduct();
        var repository = new FakeProductRepository();

        repository.AddExisting(product);

        var handler = new AddProductAliasCommandHandler(repository);

        var command = new AddProductAliasCommand(
            product.Id,
            "  Автомат Ёлка  ");

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);

        var alias = Assert.Single(product.Aliases);

        Assert.Equal("Автомат Ёлка", alias.Value);
        Assert.Equal("АВТОМАТ ЕЛКА", alias.NormalizedValue);
        Assert.NotNull(product.UpdatedAtUtc);
        Assert.Equal(1, repository.GetByIdWithDetailsCallsCount);
        Assert.Equal(1, repository.SaveChangesCallsCount);
    }

    // Проверяет, что алиас, совпадающий после нормализации,
    // не добавляется повторно и не отправляется на сохранение.
    [Fact]
    public async Task HandleRejectsNormalizedDuplicateAlias()
    {
        // Arrange
        var product = TestDataFactory.CreateProduct();
        var initialResult = product.AddAlias("Автомат Ёлка");

        Assert.True(initialResult.IsSuccess);

        var repository = new FakeProductRepository();

        repository.AddExisting(product);

        var handler = new AddProductAliasCommandHandler(repository);

        var command = new AddProductAliasCommand(
            product.Id,
            "  автомат елка  ");

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(
            "catalog.product_alias_already_exists",
            result.Error.Code);
        Assert.Single(product.Aliases);
        Assert.Equal(0, repository.SaveChangesCallsCount);
    }

    // Проверяет, что алиас длиннее доменного ограничения
    // возвращает ошибку и не изменяет коллекцию товара.
    [Fact]
    public async Task HandleRejectsAliasThatExceedsMaximumLength()
    {
        // Arrange
        var product = TestDataFactory.CreateProduct();
        var repository = new FakeProductRepository();

        repository.AddExisting(product);

        var handler = new AddProductAliasCommandHandler(repository);

        var command = new AddProductAliasCommand(
            product.Id,
            new string('A', 501));

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_too_long", result.Error.Code);
        Assert.Empty(product.Aliases);
        Assert.Equal(0, repository.SaveChangesCallsCount);
    }

    // Проверяет, что CancellationToken xUnit без изменений передаётся
    // в загрузку товара с деталями и в сохранение.
    [Fact]
    public async Task HandlePassesCancellationTokenToRepository()
    {
        // Arrange
        var product = TestDataFactory.CreateProduct();
        var repository = new FakeProductRepository();

        repository.AddExisting(product);

        var handler = new AddProductAliasCommandHandler(repository);
        var cancellationToken = TestContext.Current.CancellationToken;

        var command = new AddProductAliasCommand(
            product.Id,
            "Новый алиас");

        // Act
        var result = await handler.Handle(
            command,
            cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(
            cancellationToken,
            repository.LastGetByIdWithDetailsCancellationToken);
        Assert.Equal(
            cancellationToken,
            repository.LastSaveChangesCancellationToken);
    }
}
