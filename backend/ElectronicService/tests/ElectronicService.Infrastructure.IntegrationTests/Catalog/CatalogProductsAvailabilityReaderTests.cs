using ElectronicService.Core.Catalog.Products.SearchProducts;
using ElectronicService.Infrastructure.IntegrationTests.Data;
using ElectronicService.Infrastructure.IntegrationTests.Fixtures;
using ElectronicService.Infrastructure.Postgres.Catalog.Queries;

namespace ElectronicService.Infrastructure.IntegrationTests.Catalog;

[Collection(PostgreSqlIntegrationDefinition.Name)]
public sealed class CatalogProductsAvailabilityReaderTests
    : PostgreSqlIntegrationTest
{
    public CatalogProductsAvailabilityReaderTests(
        PostgreSqlFixture fixture)
        : base(fixture)
    {
    }

    // Проверяет OnlyInStock=true:
    // reader возвращает только товары с положительным остатком.
    [Fact]
    public async Task SearchProductsAsyncReturnsOnlyProductsInStock()
    {
        // Arrange
        await CatalogReaderTestDataFactory.CreateReaderCatalogAsync(
            DbContext,
            TestContext.Current.CancellationToken);

        var reader = new CatalogProductsReader(DbContext);

        var query = CreateQuery(onlyInStock: true);

        // Act
        var result = await reader.SearchProductsAsync(
            query,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, result.TotalCount);

        Assert.All(
            result.Items,
            item => Assert.True(item.StockQuantity > 0));
    }

    // Проверяет OnlyInStock=false:
    // reader возвращает только отсутствующие на складе товары.
    [Fact]
    public async Task SearchProductsAsyncReturnsOnlyProductsOutOfStock()
    {
        // Arrange
        await CatalogReaderTestDataFactory.CreateReaderCatalogAsync(
            DbContext,
            TestContext.Current.CancellationToken);

        var reader = new CatalogProductsReader(DbContext);

        var query = CreateQuery(onlyInStock: false);

        // Act
        var result = await reader.SearchProductsAsync(
            query,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.TotalCount);

        Assert.All(
            result.Items,
            item => Assert.Equal(0m, item.StockQuantity));
    }

    // Проверяет OnlyInStock=null:
    // фильтр не применяется и возвращаются оба состояния остатка.
    [Fact]
    public async Task SearchProductsAsyncDoesNotFilterStockWhenValueIsNull()
    {
        // Arrange
        await CatalogReaderTestDataFactory.CreateReaderCatalogAsync(
            DbContext,
            TestContext.Current.CancellationToken);

        var reader = new CatalogProductsReader(DbContext);

        var query = CreateQuery(onlyInStock: null);

        // Act
        var result = await reader.SearchProductsAsync(
            query,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(5, result.TotalCount);

        Assert.Contains(
            result.Items,
            item => item.StockQuantity > 0);

        Assert.Contains(
            result.Items,
            item => item.StockQuantity == 0);
    }

    private static SearchProductsQuery CreateQuery(
        bool? onlyInStock)
    {
        return new SearchProductsQuery(
            Search: null,
            ProductTypeCode: null,
            Manufacturer: null,
            Characteristics: [],
            Page: 1,
            PageSize: 20,
            OnlyInStock: onlyInStock);
    }
}
