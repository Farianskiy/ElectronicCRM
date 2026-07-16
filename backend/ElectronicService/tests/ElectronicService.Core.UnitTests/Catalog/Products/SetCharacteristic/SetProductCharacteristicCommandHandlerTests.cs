using ElectronicService.Core.Catalog.Products.SetCharacteristic;
using ElectronicService.Core.UnitTests.TestDoubles;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.TestCommon;

namespace ElectronicService.Core.UnitTests.Catalog.Products.SetCharacteristic;

public sealed class SetProductCharacteristicCommandHandlerTests
{
    // Проверяет, что handler отклоняет null вместо команды
    // до обращения к любому репозиторию.
    [Fact]
    public async Task HandleThrowsArgumentNullExceptionWhenCommandIsNull()
    {
        // Arrange
        var productRepository = new FakeProductRepository();
        var metadataRepository = new FakeCatalogProductMetadataRepository();

        var handler = new SetProductCharacteristicCommandHandler(
            productRepository,
            metadataRepository);

        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.Handle(null!, cancellationToken));

        // Assert
        Assert.Equal("command", exception.ParamName);
        Assert.Equal(0, productRepository.GetByIdWithDetailsCallsCount);
        Assert.Equal(0, metadataRepository.GetProductTypeByIdCallsCount);
        Assert.Equal(0, productRepository.SaveChangesCallsCount);
    }

    // Проверяет, что пустой ProductId возвращает ошибку
    // до поиска товара и метаданных.
    [Fact]
    public async Task HandleReturnsInvalidErrorWhenProductIdIsEmpty()
    {
        // Arrange
        var productRepository = new FakeProductRepository();
        var metadataRepository = new FakeCatalogProductMetadataRepository();

        var handler = new SetProductCharacteristicCommandHandler(
            productRepository,
            metadataRepository);

        var command = new SetProductCharacteristicCommand(
            Guid.Empty,
            "RATED_CURRENT",
            "16");

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_invalid", result.Error.Code);
        Assert.Equal(0, productRepository.GetByIdWithDetailsCallsCount);
        Assert.Equal(0, metadataRepository.GetProductTypeByIdCallsCount);
        Assert.Equal(0, productRepository.SaveChangesCallsCount);
    }

    // Проверяет, что пустой код характеристики отклоняется
    // до загрузки товара и метаданных.
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public async Task HandleReturnsInvalidErrorWhenCodeIsBlank(
        string code)
    {
        // Arrange
        var productRepository = new FakeProductRepository();
        var metadataRepository = new FakeCatalogProductMetadataRepository();

        var handler = new SetProductCharacteristicCommandHandler(
            productRepository,
            metadataRepository);

        var command = new SetProductCharacteristicCommand(
            Guid.NewGuid(),
            code,
            "16");

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_invalid", result.Error.Code);
        Assert.Equal(0, productRepository.GetByIdWithDetailsCallsCount);
        Assert.Equal(0, metadataRepository.GetProductTypeByIdCallsCount);
    }

    // Проверяет, что пустое значение характеристики отклоняется
    // до загрузки агрегата Product.
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public async Task HandleReturnsInvalidErrorWhenValueIsBlank(
        string value)
    {
        // Arrange
        var productRepository = new FakeProductRepository();
        var metadataRepository = new FakeCatalogProductMetadataRepository();

        var handler = new SetProductCharacteristicCommandHandler(
            productRepository,
            metadataRepository);

        var command = new SetProductCharacteristicCommand(
            Guid.NewGuid(),
            "RATED_CURRENT",
            value);

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_invalid", result.Error.Code);
        Assert.Equal(0, productRepository.GetByIdWithDetailsCallsCount);
        Assert.Equal(0, metadataRepository.GetProductTypeByIdCallsCount);
    }

    // Проверяет, что неизвестный ProductId возвращает ProductNotFound,
    // не запрашивает метаданные и не вызывает сохранение.
    [Fact]
    public async Task HandleReturnsNotFoundWhenProductDoesNotExist()
    {
        // Arrange
        var productRepository = new FakeProductRepository();
        var metadataRepository = new FakeCatalogProductMetadataRepository();

        var handler = new SetProductCharacteristicCommandHandler(
            productRepository,
            metadataRepository);

        var productId = Guid.NewGuid();

        var command = new SetProductCharacteristicCommand(
            productId,
            "RATED_CURRENT",
            "16");

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("catalog.product.not_found", result.Error.Code);
        Assert.Equal(1, productRepository.GetByIdWithDetailsCallsCount);
        Assert.Equal(0, metadataRepository.GetProductTypeByIdCallsCount);
        Assert.Equal(0, productRepository.SaveChangesCallsCount);
    }

    // Проверяет, что отсутствие ProductType возвращает специальную ошибку,
    // не запрашивает определение характеристики и не сохраняет товар.
    [Fact]
    public async Task HandleReturnsProductTypeNotFoundWhenMetadataIsMissing()
    {
        // Arrange
        var product = TestDataFactory.CreateProduct();
        var productRepository = new FakeProductRepository();
        var metadataRepository = new FakeCatalogProductMetadataRepository();

        productRepository.AddExisting(product);

        var handler = new SetProductCharacteristicCommandHandler(
            productRepository,
            metadataRepository);

        var command = new SetProductCharacteristicCommand(
            product.Id,
            "RATED_CURRENT",
            "16");

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("catalog.product_type.not_found", result.Error.Code);
        Assert.Equal(1, metadataRepository.GetProductTypeByIdCallsCount);
        Assert.Equal(
            product.ProductTypeId,
            metadataRepository.LastProductTypeId);
        Assert.Equal(
            0,
            metadataRepository.GetCharacteristicDefinitionByCodeCallsCount);
        Assert.Equal(0, productRepository.SaveChangesCallsCount);
    }

    // Проверяет, что неизвестный код характеристики возвращает
    // CharacteristicDefinitionNotFound и не изменяет товар.
    [Fact]
    public async Task HandleReturnsDefinitionNotFoundWhenCodeIsUnknown()
    {
        // Arrange
        var productType = TestDataFactory.CreateProductType();
        var product = TestDataFactory.CreateProduct(
            productTypeId: productType.Id);

        var productRepository = new FakeProductRepository();
        var metadataRepository = new FakeCatalogProductMetadataRepository();

        productRepository.AddExisting(product);
        metadataRepository.AddExisting(productType);

        var handler = new SetProductCharacteristicCommandHandler(
            productRepository,
            metadataRepository);

        var command = new SetProductCharacteristicCommand(
            product.Id,
            "UNKNOWN_CODE",
            "16");

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(
            "catalog.characteristic_definition.not_found",
            result.Error.Code);
        Assert.Equal(
            1,
            metadataRepository.GetCharacteristicDefinitionByCodeCallsCount);
        Assert.Equal("UNKNOWN_CODE", metadataRepository.LastCharacteristicCode);
        Assert.Empty(product.Characteristics);
        Assert.Equal(0, productRepository.SaveChangesCallsCount);
    }

    // Проверяет успешное создание текстовой характеристики:
    // значение обрезается, добавляется к товару и сохраняется один раз.
    [Fact]
    public async Task HandleSetsTextCharacteristicAndSavesChanges()
    {
        // Arrange
        var definition = TestDataFactory.CreateCharacteristicDefinition(
            code: "SERIES",
            name: "Серия",
            dataType: CharacteristicDataType.Text,
            unit: null);

        var productType = TestDataFactory.CreateProductType();

        TestDataFactory.AddCharacteristic(productType, definition);

        var product = TestDataFactory.CreateProduct(
            productTypeId: productType.Id);

        var productRepository = new FakeProductRepository();
        var metadataRepository = new FakeCatalogProductMetadataRepository();

        productRepository.AddExisting(product);
        metadataRepository.AddExisting(productType);
        metadataRepository.AddExisting(definition);

        var handler = new SetProductCharacteristicCommandHandler(
            productRepository,
            metadataRepository);

        var command = new SetProductCharacteristicCommand(
            product.Id,
            " series ",
            "  Proxima Ёлка  ");

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);

        var characteristic = Assert.Single(product.Characteristics);

        Assert.Equal(definition.Id, characteristic.CharacteristicDefinitionId);
        Assert.Equal(
            CharacteristicDataType.Text,
            characteristic.Value.DataType);
        Assert.Equal("Proxima Ёлка", characteristic.Value.TextValue);
        Assert.Equal(1, productRepository.SaveChangesCallsCount);
    }

    // Проверяет, что числовое значение с пробелами и запятой
    // нормализуется и сохраняется как decimal.
    [Fact]
    public async Task HandleParsesNumberWithSpacesAndComma()
    {
        // Arrange
        var definition = TestDataFactory.CreateCharacteristicDefinition(
            dataType: CharacteristicDataType.Number);

        var productType = TestDataFactory.CreateProductType();

        TestDataFactory.AddCharacteristic(productType, definition);

        var product = TestDataFactory.CreateProduct(
            productTypeId: productType.Id);

        var productRepository = new FakeProductRepository();
        var metadataRepository = new FakeCatalogProductMetadataRepository();

        productRepository.AddExisting(product);
        metadataRepository.AddExisting(productType);
        metadataRepository.AddExisting(definition);

        var handler = new SetProductCharacteristicCommandHandler(
            productRepository,
            metadataRepository);

        var command = new SetProductCharacteristicCommand(
            product.Id,
            definition.Code,
            " 1 234,50 ");

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);

        var characteristic = Assert.Single(product.Characteristics);

        Assert.Equal(
            CharacteristicDataType.Number,
            characteristic.Value.DataType);
        Assert.Equal(1_234.50m, characteristic.Value.NumberValue);
        Assert.Equal(1, productRepository.SaveChangesCallsCount);
    }

    // Проверяет, что строка, которую нельзя преобразовать в decimal,
    // возвращает ошибку и не добавляет характеристику.
    [Fact]
    public async Task HandleRejectsInvalidNumber()
    {
        // Arrange
        var definition = TestDataFactory.CreateCharacteristicDefinition(
            dataType: CharacteristicDataType.Number);

        var productType = TestDataFactory.CreateProductType();

        TestDataFactory.AddCharacteristic(productType, definition);

        var product = TestDataFactory.CreateProduct(
            productTypeId: productType.Id);

        var productRepository = new FakeProductRepository();
        var metadataRepository = new FakeCatalogProductMetadataRepository();

        productRepository.AddExisting(product);
        metadataRepository.AddExisting(productType);
        metadataRepository.AddExisting(definition);

        var handler = new SetProductCharacteristicCommandHandler(
            productRepository,
            metadataRepository);

        var command = new SetProductCharacteristicCommand(
            product.Id,
            definition.Code,
            "шестнадцать");

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_invalid", result.Error.Code);
        Assert.Empty(product.Characteristics);
        Assert.Equal(0, productRepository.SaveChangesCallsCount);
    }

    // Проверяет все поддерживаемые текстовые обозначения логических значений
    // и сохраняет ожидаемое bool-значение без отдельных дублирующихся тестов.
    [Theory]
    [InlineData("true", true)]
    [InlineData(" да ", true)]
    [InlineData("есть", true)]
    [InlineData("1", true)]
    [InlineData("+", true)]
    [InlineData("false", false)]
    [InlineData("нет", false)]
    [InlineData("отсутствует", false)]
    [InlineData("0", false)]
    [InlineData("-", false)]
    public async Task HandleParsesSupportedBooleanValues(
        string rawValue,
        bool expectedValue)
    {
        // Arrange
        var definition = TestDataFactory.CreateCharacteristicDefinition(
            code: "HAS_AUXILIARY_CONTACT",
            name: "Есть дополнительный контакт",
            dataType: CharacteristicDataType.Boolean,
            unit: null);

        var productType = TestDataFactory.CreateProductType();

        TestDataFactory.AddCharacteristic(productType, definition);

        var product = TestDataFactory.CreateProduct(
            productTypeId: productType.Id);

        var productRepository = new FakeProductRepository();
        var metadataRepository = new FakeCatalogProductMetadataRepository();

        productRepository.AddExisting(product);
        metadataRepository.AddExisting(productType);
        metadataRepository.AddExisting(definition);

        var handler = new SetProductCharacteristicCommandHandler(
            productRepository,
            metadataRepository);

        var command = new SetProductCharacteristicCommand(
            product.Id,
            definition.Code,
            rawValue);

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);

        var characteristic = Assert.Single(product.Characteristics);

        Assert.Equal(
            CharacteristicDataType.Boolean,
            characteristic.Value.DataType);
        Assert.Equal(expectedValue, characteristic.Value.BooleanValue);
        Assert.Equal(1, productRepository.SaveChangesCallsCount);
    }

    // Проверяет, что неизвестное обозначение логического значения
    // возвращает ошибку и не сохраняет изменения.
    [Fact]
    public async Task HandleRejectsUnsupportedBooleanValue()
    {
        // Arrange
        var definition = TestDataFactory.CreateCharacteristicDefinition(
            code: "HAS_AUXILIARY_CONTACT",
            name: "Есть дополнительный контакт",
            dataType: CharacteristicDataType.Boolean,
            unit: null);

        var productType = TestDataFactory.CreateProductType();

        TestDataFactory.AddCharacteristic(productType, definition);

        var product = TestDataFactory.CreateProduct(
            productTypeId: productType.Id);

        var productRepository = new FakeProductRepository();
        var metadataRepository = new FakeCatalogProductMetadataRepository();

        productRepository.AddExisting(product);
        metadataRepository.AddExisting(productType);
        metadataRepository.AddExisting(definition);

        var handler = new SetProductCharacteristicCommandHandler(
            productRepository,
            metadataRepository);

        var command = new SetProductCharacteristicCommand(
            product.Id,
            definition.Code,
            "возможно");

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_invalid", result.Error.Code);
        Assert.Empty(product.Characteristics);
        Assert.Equal(0, productRepository.SaveChangesCallsCount);
    }

    // Проверяет, что существующее определение, не разрешённое ProductType,
    // возвращает доменную ошибку и не добавляется к товару.
    [Fact]
    public async Task HandleRejectsCharacteristicNotAllowedForProductType()
    {
        // Arrange
        var definition = TestDataFactory.CreateCharacteristicDefinition();
        var productType = TestDataFactory.CreateProductType();

        var product = TestDataFactory.CreateProduct(
            productTypeId: productType.Id);

        var productRepository = new FakeProductRepository();
        var metadataRepository = new FakeCatalogProductMetadataRepository();

        productRepository.AddExisting(product);
        metadataRepository.AddExisting(productType);
        metadataRepository.AddExisting(definition);

        var handler = new SetProductCharacteristicCommandHandler(
            productRepository,
            metadataRepository);

        var command = new SetProductCharacteristicCommand(
            product.Id,
            definition.Code,
            "16");

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(
            "catalog.characteristic_is_not_allowed_for_product_type",
            result.Error.Code);
        Assert.Empty(product.Characteristics);
        Assert.Equal(0, productRepository.SaveChangesCallsCount);
    }

    // Проверяет защиту доменного агрегата от ошибки metadata repository:
    // возвращённый ProductType с чужим Id не может изменить товар.
    [Fact]
    public async Task HandleRejectsProductTypeWithDifferentId()
    {
        // Arrange
        var product = TestDataFactory.CreateProduct();
        var differentProductType = TestDataFactory.CreateProductType(
            code: "OTHER_TYPE",
            name: "Другой тип");

        var definition = TestDataFactory.CreateCharacteristicDefinition();

        TestDataFactory.AddCharacteristic(
            differentProductType,
            definition);

        var productRepository = new FakeProductRepository();
        var metadataRepository = new FakeCatalogProductMetadataRepository();

        productRepository.AddExisting(product);

        metadataRepository.AddProductTypeForLookup(
            product.ProductTypeId,
            differentProductType);

        metadataRepository.AddExisting(definition);

        var handler = new SetProductCharacteristicCommandHandler(
            productRepository,
            metadataRepository);

        var command = new SetProductCharacteristicCommand(
            product.Id,
            definition.Code,
            "16");

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_invalid", result.Error.Code);
        Assert.Empty(product.Characteristics);
        Assert.Equal(0, productRepository.SaveChangesCallsCount);
    }

    // Проверяет, что повторная установка одной характеристики
    // обновляет существующее значение вместо добавления дубликата.
    [Fact]
    public async Task HandleUpdatesExistingCharacteristicWithoutDuplicate()
    {
        // Arrange
        var definition = TestDataFactory.CreateCharacteristicDefinition();
        var productType = TestDataFactory.CreateProductType();

        TestDataFactory.AddCharacteristic(productType, definition);

        var product = TestDataFactory.CreateProduct(
            productTypeId: productType.Id);

        var initialResult = product.SetCharacteristic(
            productType,
            definition,
            TestDataFactory.CreateNumberValue(16m));

        Assert.True(initialResult.IsSuccess);

        var productRepository = new FakeProductRepository();
        var metadataRepository = new FakeCatalogProductMetadataRepository();

        productRepository.AddExisting(product);
        metadataRepository.AddExisting(productType);
        metadataRepository.AddExisting(definition);

        var handler = new SetProductCharacteristicCommandHandler(
            productRepository,
            metadataRepository);

        var command = new SetProductCharacteristicCommand(
            product.Id,
            definition.Code,
            "25");

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);

        var characteristic = Assert.Single(product.Characteristics);

        Assert.Equal(25m, characteristic.Value.NumberValue);
        Assert.Equal(1, productRepository.SaveChangesCallsCount);
    }

    // Проверяет, что CancellationToken текущего теста передаётся
    // в загрузку товара, загрузку типа, загрузку определения и сохранение.
    [Fact]
    public async Task HandlePassesCancellationTokenToAllRepositories()
    {
        // Arrange
        var definition = TestDataFactory.CreateCharacteristicDefinition();
        var productType = TestDataFactory.CreateProductType();

        TestDataFactory.AddCharacteristic(productType, definition);

        var product = TestDataFactory.CreateProduct(
            productTypeId: productType.Id);

        var productRepository = new FakeProductRepository();
        var metadataRepository = new FakeCatalogProductMetadataRepository();

        productRepository.AddExisting(product);
        metadataRepository.AddExisting(productType);
        metadataRepository.AddExisting(definition);

        var handler = new SetProductCharacteristicCommandHandler(
            productRepository,
            metadataRepository);

        var cancellationToken = TestContext.Current.CancellationToken;

        var command = new SetProductCharacteristicCommand(
            product.Id,
            definition.Code,
            "16");

        // Act
        var result = await handler.Handle(
            command,
            cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(
            cancellationToken,
            productRepository.LastGetByIdWithDetailsCancellationToken);
        Assert.Equal(
            cancellationToken,
            metadataRepository.LastProductTypeCancellationToken);
        Assert.Equal(
            cancellationToken,
            metadataRepository.LastDefinitionCancellationToken);
        Assert.Equal(
            cancellationToken,
            productRepository.LastSaveChangesCancellationToken);
    }
}
