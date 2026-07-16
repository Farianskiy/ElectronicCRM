using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.Products;
using ElectronicService.Domain.Catalog.ValueObjects;
using ElectronicService.TestCommon;

namespace ElectronicService.Domain.UnitTests.Catalog.Products;

public sealed class ProductTests
{
    // Проверяет создание корректного доступного товара и его начальное состояние.
    [Fact]
    public void CreateBuildsValidAvailableProduct()
    {
        var productTypeId = Guid.NewGuid();
        var manufacturerId = Guid.NewGuid();
        var price = TestDataFactory.CreateMoney(1_500m);
        var stock = TestDataFactory.CreateStockQuantity(5m);
        var beforeCreation = DateTime.UtcNow;

        var result = Product.Create(
            "  NB1-63-C10  ",
            "  Автоматический выключатель  ",
            productTypeId,
            manufacturerId,
            price,
            stock);

        var afterCreation = DateTime.UtcNow;

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        Assert.Equal("NB1-63-C10", result.Value.Article.Value);
        Assert.Equal("Автоматический выключатель", result.Value.Name.Value);
        Assert.Equal(productTypeId, result.Value.ProductTypeId);
        Assert.Equal(manufacturerId, result.Value.ManufacturerId);
        Assert.Same(price, result.Value.Price);
        Assert.Same(stock, result.Value.StockQuantity);
        Assert.True(result.Value.IsAvailable);
        Assert.InRange(
            result.Value.CreatedAtUtc,
            beforeCreation,
            afterCreation);
        Assert.Null(result.Value.UpdatedAtUtc);
        Assert.Empty(result.Value.Characteristics);
        Assert.Empty(result.Value.Aliases);
    }

    // Проверяет запрет пустого идентификатора типа товара.
    [Fact]
    public void CreateRejectsEmptyProductTypeId()
    {
        var result = Product.Create(
            "ARTICLE",
            "Товар",
            Guid.Empty,
            Guid.NewGuid(),
            TestDataFactory.CreateMoney(),
            TestDataFactory.CreateStockQuantity());

        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_invalid", result.Error.Code);
    }

    // Проверяет запрет пустого идентификатора производителя.
    [Fact]
    public void CreateRejectsEmptyManufacturerId()
    {
        var result = Product.Create(
            "ARTICLE",
            "Товар",
            Guid.NewGuid(),
            Guid.Empty,
            TestDataFactory.CreateMoney(),
            TestDataFactory.CreateStockQuantity());

        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_invalid", result.Error.Code);
    }

    // Проверяет обязательность цены при создании товара.
    [Fact]
    public void CreateRejectsNullPrice()
    {
        var result = Product.Create(
            "ARTICLE",
            "Товар",
            Guid.NewGuid(),
            Guid.NewGuid(),
            null!,
            TestDataFactory.CreateStockQuantity());

        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_invalid", result.Error.Code);
    }

    // Проверяет переименование товара и установку времени изменения.
    [Fact]
    public void RenameChangesNameAndUpdatedTimestamp()
    {
        var product = TestDataFactory.CreateProduct();

        var result = product.Rename("  Новое название  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Новое название", product.Name.Value);
        Assert.NotNull(product.UpdatedAtUtc);
    }

    // Проверяет сохранение старого названия при невалидном переименовании.
    [Fact]
    public void RenameKeepsOldNameWhenNewNameIsInvalid()
    {
        var product = TestDataFactory.CreateProduct();
        var oldName = product.Name;

        var result = product.Rename(" ");

        Assert.True(result.IsFailure);
        Assert.Same(oldName, product.Name);
        Assert.Null(product.UpdatedAtUtc);
    }

    // Проверяет изменение артикула и установку времени изменения.
    [Fact]
    public void ChangeArticleChangesArticleAndUpdatedTimestamp()
    {
        var product = TestDataFactory.CreateProduct();

        var result = product.ChangeArticle("  NEW-ARTICLE  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("NEW-ARTICLE", product.Article.Value);
        Assert.NotNull(product.UpdatedAtUtc);
    }

    // Проверяет замену цены товара.
    [Fact]
    public void ChangePriceChangesPrice()
    {
        var product = TestDataFactory.CreateProduct(price: 100m);
        var newPrice = TestDataFactory.CreateMoney(250m);

        var result = product.ChangePrice(newPrice);

        Assert.True(result.IsSuccess);
        Assert.Same(newPrice, product.Price);
        Assert.Equal(250m, product.Price.Amount);
        Assert.NotNull(product.UpdatedAtUtc);
    }

    // Проверяет изменение остатка и вычисляемой доступности товара.
    [Fact]
    public void ChangeStockQuantityChangesAvailability()
    {
        var product = TestDataFactory.CreateProduct(stockQuantity: 10m);
        var emptyStock = StockQuantity.Zero();

        var result = product.ChangeStockQuantity(emptyStock);

        Assert.True(result.IsSuccess);
        Assert.False(product.IsAvailable);
        Assert.Same(emptyStock, product.StockQuantity);
        Assert.NotNull(product.UpdatedAtUtc);
    }

    // Проверяет добавление разрешённой характеристики к товару.
    [Fact]
    public void SetCharacteristicAddsAllowedValue()
    {
        var definition = TestDataFactory.CreateCharacteristicDefinition();
        var productType = TestDataFactory.CreateProductType();

        TestDataFactory.AddCharacteristic(productType, definition);

        var product = TestDataFactory.CreateProduct(
            productTypeId: productType.Id);
        var value = TestDataFactory.CreateNumberValue(16m);

        var result = product.SetCharacteristic(
            productType,
            definition,
            value);

        Assert.True(result.IsSuccess);

        var characteristic = Assert.Single(product.Characteristics);

        Assert.Equal(definition.Id, characteristic.CharacteristicDefinitionId);
        Assert.Same(value, characteristic.Value);
        Assert.NotNull(product.UpdatedAtUtc);
    }

    // Проверяет обновление существующей характеристики без создания дубликата.
    [Fact]
    public void SetCharacteristicUpdatesExistingValueWithoutDuplicate()
    {
        var definition = TestDataFactory.CreateCharacteristicDefinition();
        var productType = TestDataFactory.CreateProductType();

        TestDataFactory.AddCharacteristic(productType, definition);

        var product = TestDataFactory.CreateProduct(
            productTypeId: productType.Id);

        var firstResult = product.SetCharacteristic(
            productType,
            definition,
            TestDataFactory.CreateNumberValue(16m));

        var secondValue = TestDataFactory.CreateNumberValue(25m);

        var secondResult = product.SetCharacteristic(
            productType,
            definition,
            secondValue);

        Assert.True(firstResult.IsSuccess);
        Assert.True(secondResult.IsSuccess);

        var characteristic = Assert.Single(product.Characteristics);

        Assert.Same(secondValue, characteristic.Value);
        Assert.Equal(25m, characteristic.Value.NumberValue);
    }

    // Проверяет запрет изменения товара через другой тип товара.
    [Fact]
    public void SetCharacteristicRejectsProductTypeWithDifferentId()
    {
        var product = TestDataFactory.CreateProduct();
        var differentProductType = TestDataFactory.CreateProductType();
        var definition = TestDataFactory.CreateCharacteristicDefinition();

        TestDataFactory.AddCharacteristic(
            differentProductType,
            definition);

        var result = product.SetCharacteristic(
            differentProductType,
            definition,
            TestDataFactory.CreateNumberValue());

        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_invalid", result.Error.Code);
        Assert.Empty(product.Characteristics);
    }

    // Проверяет запрет характеристики, не разрешённой для типа товара.
    [Fact]
    public void SetCharacteristicRejectsDefinitionNotAllowedForType()
    {
        var productType = TestDataFactory.CreateProductType();
        var definition = TestDataFactory.CreateCharacteristicDefinition();
        var product = TestDataFactory.CreateProduct(
            productTypeId: productType.Id);

        var result = product.SetCharacteristic(
            productType,
            definition,
            TestDataFactory.CreateNumberValue());

        Assert.True(result.IsFailure);
        Assert.Equal(
            "catalog.characteristic_is_not_allowed_for_product_type",
            result.Error.Code);
        Assert.Empty(product.Characteristics);
    }

    // Проверяет запрет значения с неподходящим типом данных.
    [Fact]
    public void SetCharacteristicRejectsValueWithWrongDataType()
    {
        var definition = TestDataFactory.CreateCharacteristicDefinition(
            dataType: CharacteristicDataType.Number);
        var productType = TestDataFactory.CreateProductType();

        TestDataFactory.AddCharacteristic(productType, definition);

        var product = TestDataFactory.CreateProduct(
            productTypeId: productType.Id);

        var result = product.SetCharacteristic(
            productType,
            definition,
            TestDataFactory.CreateTextValue("16"));

        Assert.True(result.IsFailure);
        Assert.Equal(
            "catalog.characteristic_value_type_mismatch",
            result.Error.Code);
        Assert.Empty(product.Characteristics);
    }

    // Проверяет удаление необязательной характеристики товара.
    [Fact]
    public void RemoveCharacteristicRemovesOptionalCharacteristic()
    {
        var definition = TestDataFactory.CreateCharacteristicDefinition();
        var productType = TestDataFactory.CreateProductType();

        TestDataFactory.AddCharacteristic(
            productType,
            definition,
            isRequired: false);

        var product = TestDataFactory.CreateProduct(
            productTypeId: productType.Id);

        product.SetCharacteristic(
            productType,
            definition,
            TestDataFactory.CreateNumberValue());

        var result = product.RemoveCharacteristic(
            productType,
            definition.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(product.Characteristics);
    }

    // Проверяет запрет удаления обязательной характеристики.
    [Fact]
    public void RemoveCharacteristicRejectsRequiredCharacteristic()
    {
        var definition = TestDataFactory.CreateCharacteristicDefinition();
        var productType = TestDataFactory.CreateProductType();

        TestDataFactory.AddCharacteristic(
            productType,
            definition,
            isRequired: true);

        var product = TestDataFactory.CreateProduct(
            productTypeId: productType.Id);

        product.SetCharacteristic(
            productType,
            definition,
            TestDataFactory.CreateNumberValue());

        var result = product.RemoveCharacteristic(
            productType,
            definition.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_invalid", result.Error.Code);
        Assert.Single(product.Characteristics);
    }

    // Проверяет ошибку при отсутствии обязательной характеристики.
    [Fact]
    public void ValidateRequiredCharacteristicsReturnsMissingError()
    {
        var definition = TestDataFactory.CreateCharacteristicDefinition();
        var productType = TestDataFactory.CreateProductType();

        TestDataFactory.AddCharacteristic(
            productType,
            definition,
            isRequired: true);

        var product = TestDataFactory.CreateProduct(
            productTypeId: productType.Id);

        var result = product.ValidateRequiredCharacteristics(productType);

        Assert.True(result.IsFailure);
        Assert.Equal(
            "catalog.required_characteristic_is_missing",
            result.Error.Code);
    }

    // Проверяет успешную валидацию заполненных обязательных характеристик.
    [Fact]
    public void ValidateRequiredCharacteristicsReturnsSuccessWhenAllPresent()
    {
        var definition = TestDataFactory.CreateCharacteristicDefinition();
        var productType = TestDataFactory.CreateProductType();

        TestDataFactory.AddCharacteristic(
            productType,
            definition,
            isRequired: true);

        var product = TestDataFactory.CreateProduct(
            productTypeId: productType.Id);

        product.SetCharacteristic(
            productType,
            definition,
            TestDataFactory.CreateNumberValue());

        var result = product.ValidateRequiredCharacteristics(productType);

        Assert.True(result.IsSuccess);
    }

    // Проверяет добавление, очистку и нормализацию алиаса товара.
    [Fact]
    public void AddAliasStoresTrimmedAndNormalizedAlias()
    {
        var product = TestDataFactory.CreateProduct();

        var result = product.AddAlias("  Автомат Ёлка  ");

        Assert.True(result.IsSuccess);

        var alias = Assert.Single(product.Aliases);

        Assert.Equal("Автомат Ёлка", alias.Value);
        Assert.Equal("АВТОМАТ ЕЛКА", alias.NormalizedValue);
    }

    // Проверяет запрет алиасов-дубликатов после нормализации.
    [Fact]
    public void AddAliasRejectsNormalizedDuplicate()
    {
        var product = TestDataFactory.CreateProduct();

        var firstResult = product.AddAlias("Автомат Ёлка");
        var secondResult = product.AddAlias("  автомат елка  ");

        Assert.True(firstResult.IsSuccess);
        Assert.True(secondResult.IsFailure);
        Assert.Equal(
            "catalog.product_alias_already_exists",
            secondResult.Error.Code);
        Assert.Single(product.Aliases);
    }
}
