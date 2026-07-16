using ElectronicService.Core.Catalog.Products.SearchProducts;
using ElectronicService.Infrastructure.IntegrationTests.Data;
using ElectronicService.Infrastructure.IntegrationTests.Fixtures;
using ElectronicService.Infrastructure.Postgres.Catalog.Queries;

namespace ElectronicService.Infrastructure.IntegrationTests.Catalog;

[Collection(PostgreSqlIntegrationDefinition.Name)]
public sealed class CatalogProductsReaderTests
    : PostgreSqlIntegrationTest
{
    public CatalogProductsReaderTests(PostgreSqlFixture fixture)
        : base(fixture)
    {
    }

    // Проверяет пагинацию, общее количество и сортировку:
    // вторая страница по два элемента содержит третий и четвёртый товары.
    [Fact]
    public async Task GetProductsAsyncReturnsRequestedPageAndTotalCount()
    {
        // Arrange
        var data = await CreateCatalogAsync();
        var reader = new CatalogProductsReader(DbContext);

        // Act
        var result = await reader.GetProductsAsync(
            search: null,
            productTypeCode: null,
            manufacturer: null,
            page: 2,
            pageSize: 2,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(5, result.TotalCount);

        Assert.Collection(
            result.Items,
            item => Assert.Equal(data.Delta.Id, item.Id),
            item => Assert.Equal(data.Epsilon.Id, item.Id));
    }

    // Проверяет поиск по нормализованному названию товара
    // без учёта регистра строки поиска.
    [Fact]
    public async Task GetProductsAsyncFindsProductByName()
    {
        // Arrange
        var data = await CreateCatalogAsync();
        var reader = new CatalogProductsReader(DbContext);

        // Act
        var result = await reader.GetProductsAsync(
            search: "alpha breaker",
            productTypeCode: null,
            manufacturer: null,
            page: 1,
            pageSize: 20,
            TestContext.Current.CancellationToken);

        // Assert
        var item = Assert.Single(result.Items);

        Assert.Equal(data.Alpha.Id, item.Id);
        Assert.Equal(data.Alpha.Name.Value, item.Name);
        Assert.Equal(1, result.TotalCount);
    }

    // Проверяет поиск по артикулу через PostgreSQL ILIKE.
    [Fact]
    public async Task GetProductsAsyncFindsProductByArticle()
    {
        // Arrange
        var data = await CreateCatalogAsync();
        var reader = new CatalogProductsReader(DbContext);

        // Act
        var result = await reader.GetProductsAsync(
            search: data.Beta.Article.Value,
            productTypeCode: null,
            manufacturer: null,
            page: 1,
            pageSize: 20,
            TestContext.Current.CancellationToken);

        // Assert
        var item = Assert.Single(result.Items);

        Assert.Equal(data.Beta.Id, item.Id);
        Assert.Equal(data.Beta.Article.Value, item.Article);
    }

    // Проверяет поиск по альтернативному названию ProductAlias.
    [Fact]
    public async Task GetProductsAsyncFindsProductByAlias()
    {
        // Arrange
        var data = await CreateCatalogAsync();
        var reader = new CatalogProductsReader(DbContext);

        // Act
        var result = await reader.GetProductsAsync(
            search: "special reader alias",
            productTypeCode: null,
            manufacturer: null,
            page: 1,
            pageSize: 20,
            TestContext.Current.CancellationToken);

        // Assert
        var item = Assert.Single(result.Items);

        Assert.Equal(data.Alpha.Id, item.Id);
    }

    // Проверяет частичный фильтр по нормализованному имени производителя.
    [Fact]
    public async Task GetProductsAsyncFiltersByManufacturer()
    {
        // Arrange
        var data = await CreateCatalogAsync();
        var reader = new CatalogProductsReader(DbContext);

        // Act
        var result = await reader.GetProductsAsync(
            search: null,
            productTypeCode: null,
            manufacturer: "iek reader",
            page: 1,
            pageSize: 20,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, result.TotalCount);

        Assert.Contains(
            result.Items,
            item => item.Id == data.Alpha.Id);

        Assert.Contains(
            result.Items,
            item => item.Id == data.Beta.Id);

        Assert.Contains(
            result.Items,
            item => item.Id == data.Epsilon.Id);

        Assert.DoesNotContain(
            result.Items,
            item => item.Id == data.Delta.Id);
    }

    // Проверяет фильтр по коду типа товара.
    [Fact]
    public async Task GetProductsAsyncFiltersByProductTypeCode()
    {
        // Arrange
        var data = await CreateCatalogAsync();
        var reader = new CatalogProductsReader(DbContext);

        // Act
        var result = await reader.GetProductsAsync(
            search: null,
            productTypeCode: $"  {data.BreakerType.Code}  ",
            manufacturer: null,
            page: 1,
            pageSize: 20,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, result.TotalCount);

        Assert.All(
            result.Items,
            item => Assert.Equal(
                data.BreakerType.Code,
                item.ProductTypeCode));
    }

    // Проверяет, что reader возвращает точный остаток
    // и для доступного, и для отсутствующего на складе товара.
    [Fact]
    public async Task GetProductsAsyncReturnsPositiveAndZeroStockQuantities()
    {
        // Arrange
        var data = await CreateCatalogAsync();
        var reader = new CatalogProductsReader(DbContext);

        // Act
        var result = await reader.GetProductsAsync(
            search: null,
            productTypeCode: null,
            manufacturer: null,
            page: 1,
            pageSize: 20,
            TestContext.Current.CancellationToken);

        // Assert
        var alpha = Assert.Single(
            result.Items,
            item => item.Id == data.Alpha.Id);

        var beta = Assert.Single(
            result.Items,
            item => item.Id == data.Beta.Id);

        Assert.Equal(10m, alpha.StockQuantity);
        Assert.Equal(0m, beta.StockQuantity);
    }

    // Проверяет фильтрацию по текстовой характеристике
    // с частичным совпадением без учёта регистра.
    [Fact]
    public async Task SearchProductsAsyncFiltersByTextCharacteristic()
    {
        // Arrange
        var data = await CreateCatalogAsync();
        var reader = new CatalogProductsReader(DbContext);

        var query = new SearchProductsQuery(
            Search: null,
            ProductTypeCode: null,
            Manufacturer: null,
            Characteristics:
            [
                new SearchProductCharacteristicFilter(
                    data.SeriesDefinition.Code,
                    "proxima")
            ],
            Page: 1,
            PageSize: 20);

        // Act
        var result = await reader.SearchProductsAsync(
            query,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.TotalCount);

        Assert.Contains(
            result.Items,
            item => item.Id == data.Alpha.Id);

        Assert.Contains(
            result.Items,
            item => item.Id == data.Delta.Id);
    }

    // Проверяет разбор числа с десятичной запятой
    // и точное сравнение числовой характеристики.
    [Fact]
    public async Task SearchProductsAsyncFiltersByNumberCharacteristic()
    {
        // Arrange
        var data = await CreateCatalogAsync();
        var reader = new CatalogProductsReader(DbContext);

        var query = new SearchProductsQuery(
            Search: null,
            ProductTypeCode: null,
            Manufacturer: null,
            Characteristics:
            [
                new SearchProductCharacteristicFilter(
                    data.CurrentDefinition.Code,
                    "16,5")
            ],
            Page: 1,
            PageSize: 20);

        // Act
        var result = await reader.SearchProductsAsync(
            query,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.TotalCount);

        Assert.DoesNotContain(
            result.Items,
            item => item.Id == data.Beta.Id);
    }

    // Проверяет распознавание русского логического значения
    // и фильтрацию Boolean-характеристики.
    [Fact]
    public async Task SearchProductsAsyncFiltersByBooleanCharacteristic()
    {
        // Arrange
        var data = await CreateCatalogAsync();
        var reader = new CatalogProductsReader(DbContext);

        var query = new SearchProductsQuery(
            Search: null,
            ProductTypeCode: null,
            Manufacturer: null,
            Characteristics:
            [
                new SearchProductCharacteristicFilter(
                    data.AuxiliaryDefinition.Code,
                    "да")
            ],
            Page: 1,
            PageSize: 20);

        // Act
        var result = await reader.SearchProductsAsync(
            query,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.TotalCount);

        Assert.Contains(
            result.Items,
            item => item.Id == data.Alpha.Id);

        Assert.Contains(
            result.Items,
            item => item.Id == data.Delta.Id);
    }

    // Проверяет, что несколько фильтров характеристик
    // применяются одновременно по логике AND.
    [Fact]
    public async Task SearchProductsAsyncCombinesCharacteristicFilters()
    {
        // Arrange
        var data = await CreateCatalogAsync();
        var reader = new CatalogProductsReader(DbContext);

        var query = new SearchProductsQuery(
            Search: null,
            ProductTypeCode: null,
            Manufacturer: data.IekManufacturer.Name,
            Characteristics:
            [
                new SearchProductCharacteristicFilter(
                    data.SeriesDefinition.Code,
                    "Proxima"),
                new SearchProductCharacteristicFilter(
                    data.CurrentDefinition.Code,
                    "16.5"),
                new SearchProductCharacteristicFilter(
                    data.AuxiliaryDefinition.Code,
                    "true")
            ],
            Page: 1,
            PageSize: 20);

        // Act
        var result = await reader.SearchProductsAsync(
            query,
            TestContext.Current.CancellationToken);

        // Assert
        var item = Assert.Single(result.Items);

        Assert.Equal(data.Alpha.Id, item.Id);
        Assert.Equal(1, result.TotalCount);
    }

    // Проверяет безопасное поведение при неизвестном коде характеристики:
    // reader возвращает пустую страницу вместо исключения.
    [Fact]
    public async Task SearchProductsAsyncReturnsEmptyPageForUnknownCharacteristic()
    {
        // Arrange
        await CreateCatalogAsync();
        var reader = new CatalogProductsReader(DbContext);

        var query = new SearchProductsQuery(
            Search: null,
            ProductTypeCode: null,
            Manufacturer: null,
            Characteristics:
            [
                new SearchProductCharacteristicFilter(
                    "UNKNOWN_CHARACTERISTIC_CODE",
                    "16")
            ],
            Page: 1,
            PageSize: 20);

        // Act
        var result = await reader.SearchProductsAsync(
            query,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    // Проверяет подробную карточку:
    // основные поля, три типа характеристик и упорядоченные алиасы.
    [Fact]
    public async Task GetProductByIdAsyncReturnsDetailsCharacteristicsAndAliases()
    {
        // Arrange
        var data = await CreateCatalogAsync();
        var reader = new CatalogProductsReader(DbContext);

        // Act
        var result = await reader.GetProductByIdAsync(
            data.Alpha.Id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(data.Alpha.Id, result.Id);
        Assert.Equal(data.Alpha.Article.Value, result.Article);
        Assert.Equal(data.Alpha.Name.Value, result.Name);
        Assert.Equal(data.BreakerType.Code, result.ProductTypeCode);
        Assert.Equal(data.IekManufacturer.Name, result.ManufacturerName);
        Assert.Equal(100m, result.PriceAmount);
        Assert.Equal("RUB", result.PriceCurrency);
        Assert.Equal(10m, result.StockQuantity);

        var textCharacteristic = Assert.Single(
            result.Characteristics,
            item => StringComparer.Ordinal.Equals(
                item.Code,
                data.SeriesDefinition.Code));

        var numberCharacteristic = Assert.Single(
            result.Characteristics,
            item => StringComparer.Ordinal.Equals(
                item.Code,
                data.CurrentDefinition.Code));

        var booleanCharacteristic = Assert.Single(
            result.Characteristics,
            item => StringComparer.Ordinal.Equals(
                item.Code,
                data.AuxiliaryDefinition.Code));

        Assert.Equal("Text", textCharacteristic.DataType);
        Assert.Equal("Proxima", textCharacteristic.Value);

        Assert.Equal("Number", numberCharacteristic.DataType);
        Assert.Equal("16.5", numberCharacteristic.Value);
        Assert.Equal("А", numberCharacteristic.Unit);

        Assert.Equal("Boolean", booleanCharacteristic.DataType);
        Assert.Equal("Да", booleanCharacteristic.Value);

        Assert.Collection(
            result.Aliases,
            alias => Assert.StartsWith(
                "Alpha Alias",
                alias,
                StringComparison.Ordinal),
            alias => Assert.StartsWith(
                "Special Reader Alias",
                alias,
                StringComparison.Ordinal));
    }

    // Проверяет, что запрос неизвестного ProductId
    // возвращает null и не формирует пустую карточку.
    [Fact]
    public async Task GetProductByIdAsyncReturnsNullForUnknownProduct()
    {
        // Arrange
        var reader = new CatalogProductsReader(DbContext);

        // Act
        var result = await reader.GetProductByIdAsync(
            Guid.NewGuid(),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    private Task<ReaderCatalogData> CreateCatalogAsync()
    {
        return CatalogReaderTestDataFactory.CreateReaderCatalogAsync(
            DbContext,
            TestContext.Current.CancellationToken);
    }
}
