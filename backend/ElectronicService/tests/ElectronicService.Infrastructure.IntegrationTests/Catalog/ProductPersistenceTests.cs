using ElectronicService.Infrastructure.IntegrationTests.Data;
using ElectronicService.Infrastructure.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.IntegrationTests.Catalog;

[Collection(PostgreSqlIntegrationDefinition.Name)]
public sealed class ProductPersistenceTests : PostgreSqlIntegrationTest
{
    public ProductPersistenceTests(PostgreSqlFixture fixture)
        : base(fixture)
    {
    }

    // Проверяет реальные EF Core mappings owned Value Objects:
    // Article, Name, Money и StockQuantity сохраняются в products
    // и восстанавливаются без потери значений.
    [Fact]
    public async Task SaveAndReloadPersistsProductOwnedValueObjects()
    {
        // Arrange
        var graph =
            PostgreSqlTestDataFactory.CreateCatalogProductGraph();

        await SaveGraphAsync(graph);

        DbContext.ChangeTracker.Clear();

        // Act
        var loadedProduct = await DbContext.Products.SingleAsync(
            product => product.Id == graph.Product.Id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(
            graph.Product.Article.Value,
            loadedProduct.Article.Value);

        Assert.Equal(
            graph.Product.Name.Value,
            loadedProduct.Name.Value);

        Assert.Equal(
            graph.Product.Name.NormalizedValue,
            loadedProduct.Name.NormalizedValue);

        Assert.Equal(1_234.56m, loadedProduct.Price.Amount);
        Assert.Equal("RUB", loadedProduct.Price.Currency);
        Assert.Equal(7.125m, loadedProduct.StockQuantity.Value);
    }

    // Проверяет database cascade delete:
    // удаление Product удаляет связанные ProductAlias
    // и ProductCharacteristic даже когда зависимости не загружены в ChangeTracker.
    [Fact]
    public async Task DeletingProductCascadesAliasesAndCharacteristics()
    {
        // Arrange
        var graph =
            PostgreSqlTestDataFactory.CreateCatalogProductGraph();

        await SaveGraphAsync(graph);

        DbContext.ChangeTracker.Clear();

        var product = await DbContext.Products.SingleAsync(
            item => item.Id == graph.Product.Id,
            TestContext.Current.CancellationToken);

        // Act
        DbContext.Products.Remove(product);

        await DbContext.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        DbContext.ChangeTracker.Clear();

        // Assert
        var aliasExists = await DbContext.ProductAliases.AnyAsync(
            alias => alias.ProductId == graph.Product.Id,
            TestContext.Current.CancellationToken);

        var characteristicExists =
            await DbContext.ProductCharacteristics.AnyAsync(
                characteristic =>
                    characteristic.ProductId == graph.Product.Id,
                TestContext.Current.CancellationToken);

        Assert.False(aliasExists);
        Assert.False(characteristicExists);
    }

    // Проверяет уникальный индекс manufacturers.normalized_name:
    // разные варианты регистра одного имени не могут сохраниться дважды.
    [Fact]
    public async Task DatabaseRejectsDuplicateManufacturerNormalizedName()
    {
        // Arrange
        var suffix = Guid.NewGuid().ToString("N");

        var first =
            PostgreSqlTestDataFactory.CreateManufacturer(
                $"Unique Manufacturer {suffix}");

        var second =
            PostgreSqlTestDataFactory.CreateManufacturer(
                $"unique manufacturer {suffix}");

        DbContext.Manufacturers.AddRange(first, second);

        // Act
        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            async () =>
            {
                await DbContext.SaveChangesAsync(
                    TestContext.Current.CancellationToken);
            });

        // Assert
        Assert.NotNull(exception.InnerException);
    }

    // Проверяет Restrict foreign key Product -> Manufacturer:
    // производителя нельзя удалить, пока на него ссылается товар.
    [Fact]
    public async Task DatabaseRejectsDeletingReferencedManufacturer()
    {
        // Arrange
        var graph =
            PostgreSqlTestDataFactory.CreateCatalogProductGraph();

        await SaveGraphAsync(graph);

        DbContext.ChangeTracker.Clear();

        var manufacturer =
            await DbContext.Manufacturers.SingleAsync(
                item => item.Id == graph.Manufacturer.Id,
                TestContext.Current.CancellationToken);

        // Act
        DbContext.Manufacturers.Remove(manufacturer);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            async () =>
            {
                await DbContext.SaveChangesAsync(
                    TestContext.Current.CancellationToken);
            });

        // Assert
        Assert.NotNull(exception.InnerException);
    }
}
