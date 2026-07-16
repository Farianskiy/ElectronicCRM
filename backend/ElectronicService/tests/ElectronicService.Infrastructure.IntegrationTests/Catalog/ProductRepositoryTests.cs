using ElectronicService.Infrastructure.IntegrationTests.Data;
using ElectronicService.Infrastructure.IntegrationTests.Fixtures;
using ElectronicService.Infrastructure.Postgres.Catalog.Repositories;
using ElectronicService.TestCommon;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.IntegrationTests.Catalog;

[Collection(PostgreSqlIntegrationDefinition.Name)]
public sealed class ProductRepositoryTests : PostgreSqlIntegrationTest
{
    public ProductRepositoryTests(PostgreSqlFixture fixture)
        : base(fixture)
    {
    }

    // Проверяет настоящий ProductRepository.GetByIdAsync:
    // товар и owned Value Objects загружаются,
    // а коллекции Aliases и Characteristics не включаются автоматически.
    [Fact]
    public async Task GetByIdAsyncReturnsProductWithoutLoadingDetails()
    {
        // Arrange
        var graph =
            PostgreSqlTestDataFactory.CreateCatalogProductGraph();

        await SaveGraphAsync(graph);

        DbContext.ChangeTracker.Clear();

        var repository = new ProductRepository(DbContext);

        // Act
        var product = await repository.GetByIdAsync(
            graph.Product.Id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(product);
        Assert.Equal(graph.Product.Id, product.Id);
        Assert.Equal(graph.Product.Article.Value, product.Article.Value);

        Assert.False(
            DbContext.Entry(product)
                .Collection(item => item.Aliases)
                .IsLoaded);

        Assert.False(
            DbContext.Entry(product)
                .Collection(item => item.Characteristics)
                .IsLoaded);
    }

    // Проверяет настоящий GetByIdWithDetailsAsync:
    // repository использует Include и загружает алиасы и характеристики.
    [Fact]
    public async Task GetByIdWithDetailsAsyncLoadsAliasesAndCharacteristics()
    {
        // Arrange
        var graph =
            PostgreSqlTestDataFactory.CreateCatalogProductGraph();

        await SaveGraphAsync(graph);

        DbContext.ChangeTracker.Clear();

        var repository = new ProductRepository(DbContext);

        // Act
        var product = await repository.GetByIdWithDetailsAsync(
            graph.Product.Id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(product);

        var alias = Assert.Single(product.Aliases);
        var characteristic = Assert.Single(product.Characteristics);

        Assert.Equal(
            graph.Product.Aliases.Single().NormalizedValue,
            alias.NormalizedValue);

        Assert.Equal(
            graph.Definition.Id,
            characteristic.CharacteristicDefinitionId);

        Assert.Equal(
            16.5m,
            characteristic.Value.NumberValue);

        Assert.True(
            DbContext.Entry(product)
                .Collection(item => item.Aliases)
                .IsLoaded);

        Assert.True(
            DbContext.Entry(product)
                .Collection(item => item.Characteristics)
                .IsLoaded);
    }

    // Проверяет настоящий SaveChangesAsync репозитория:
    // доменные изменения Price и StockQuantity доходят до PostgreSQL.
    [Fact]
    public async Task SaveChangesAsyncPersistsDomainChanges()
    {
        // Arrange
        var graph =
            PostgreSqlTestDataFactory.CreateCatalogProductGraph();

        await SaveGraphAsync(graph);

        DbContext.ChangeTracker.Clear();

        var repository = new ProductRepository(DbContext);

        var product = await repository.GetByIdAsync(
            graph.Product.Id,
            TestContext.Current.CancellationToken);

        Assert.NotNull(product);

        var priceResult = product.ChangePrice(
            TestDataFactory.CreateMoney(2_500m, "USD"));

        var stockResult = product.ChangeStockQuantity(
            TestDataFactory.CreateStockQuantity(0m));

        Assert.True(priceResult.IsSuccess);
        Assert.True(stockResult.IsSuccess);

        // Act
        await repository.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        DbContext.ChangeTracker.Clear();

        var reloaded = await DbContext.Products.SingleAsync(
            item => item.Id == graph.Product.Id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2_500m, reloaded.Price.Amount);
        Assert.Equal("USD", reloaded.Price.Currency);
        Assert.Equal(0m, reloaded.StockQuantity.Value);
        Assert.False(reloaded.IsAvailable);
        Assert.NotNull(reloaded.UpdatedAtUtc);
    }

    // Проверяет, что настоящий repository возвращает null
    // для отсутствующего ProductId и не создаёт фиктивный объект.
    [Fact]
    public async Task GetByIdAsyncReturnsNullForUnknownProduct()
    {
        // Arrange
        var repository = new ProductRepository(DbContext);

        // Act
        var product = await repository.GetByIdAsync(
            Guid.NewGuid(),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(product);
    }
}
