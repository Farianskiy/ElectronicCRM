using ElectronicService.Core.Catalog.Products.GetReplacements;
using ElectronicService.Infrastructure.IntegrationTests.Data;
using ElectronicService.Infrastructure.IntegrationTests.Fixtures;
using ElectronicService.Infrastructure.Postgres.Catalog.Queries;

namespace ElectronicService.Infrastructure.IntegrationTests.Catalog;

[Collection(PostgreSqlIntegrationDefinition.Name)]
public sealed class CatalogProductReplacementsReaderTests
    : PostgreSqlIntegrationTest
{
    public CatalogProductReplacementsReaderTests(
        PostgreSqlFixture fixture)
        : base(fixture)
    {
    }

    // Проверяет, что неизвестный исходный ProductId
    // возвращает null до вычисления кандидатов.
    [Fact]
    public async Task GetReplacementsAsyncReturnsNullForUnknownProduct()
    {
        // Arrange
        var reader =
            new CatalogProductReplacementsReader(DbContext);

        var query = new GetProductReplacementsQuery(
            ProductId: Guid.NewGuid(),
            OnlyInStock: false,
            MinimumScore: 0m,
            Page: 1,
            PageSize: 20);

        // Act
        var result = await reader.GetReplacementsAsync(
            query,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    // Проверяет MinimumScore и итоговый ReplacementScore:
    // два полностью совместимых кандидата получают 100,
    // частичный кандидат с результатом 60 исключается порогом 90.
    [Fact]
    public async Task GetReplacementsAsyncCalculatesScoresAndAppliesMinimum()
    {
        // Arrange
        var data = await CreateCatalogAsync();

        var reader =
            new CatalogProductReplacementsReader(DbContext);

        var query = new GetProductReplacementsQuery(
            ProductId: data.Target.Id,
            OnlyInStock: false,
            MinimumScore: 90m,
            Page: 1,
            PageSize: 20);

        // Act
        var result = await reader.GetReplacementsAsync(
            query,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(data.Target.Id, result.ProductId);
        Assert.Equal(2, result.TotalCount);

        Assert.Collection(
            result.Items,
            item =>
            {
                Assert.Equal(data.PerfectInStock.Id, item.Id);
                Assert.Equal(100m, item.ReplacementScore);
            },
            item =>
            {
                Assert.Equal(data.PerfectOutOfStock.Id, item.Id);
                Assert.Equal(100m, item.ReplacementScore);
            });
    }

    // Проверяет OnlyInStock:
    // кандидат с нулевым остатком исключается до подсчёта результата.
    [Fact]
    public async Task GetReplacementsAsyncFiltersOutOfStockCandidates()
    {
        // Arrange
        var data = await CreateCatalogAsync();

        var reader =
            new CatalogProductReplacementsReader(DbContext);

        var query = new GetProductReplacementsQuery(
            ProductId: data.Target.Id,
            OnlyInStock: true,
            MinimumScore: 50m,
            Page: 1,
            PageSize: 20);

        // Act
        var result = await reader.GetReplacementsAsync(
            query,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);

        Assert.Contains(
            result.Items,
            item => item.Id == data.PerfectInStock.Id
                && item.ReplacementScore == 100m);

        Assert.Contains(
            result.Items,
            item => item.Id == data.Partial.Id
                && item.ReplacementScore == 60m);

        Assert.DoesNotContain(
            result.Items,
            item => item.Id == data.PerfectOutOfStock.Id);
    }

    // Проверяет пагинацию после сортировки по score:
    // вторая страница содержит частичный и низкий варианты,
    // а TotalCount отражает всех кандидатов подходящего типа.
    [Fact]
    public async Task GetReplacementsAsyncPaginatesScoredCandidates()
    {
        // Arrange
        var data = await CreateCatalogAsync();

        var reader =
            new CatalogProductReplacementsReader(DbContext);

        var query = new GetProductReplacementsQuery(
            ProductId: data.Target.Id,
            OnlyInStock: false,
            MinimumScore: 0m,
            Page: 2,
            PageSize: 2);

        // Act
        var result = await reader.GetReplacementsAsync(
            query,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(4, result.TotalCount);

        Assert.Collection(
            result.Items,
            item =>
            {
                Assert.Equal(data.Partial.Id, item.Id);
                Assert.Equal(60m, item.ReplacementScore);
            },
            item =>
            {
                Assert.Equal(data.LowScore.Id, item.Id);
                Assert.Equal(0m, item.ReplacementScore);
            });

        Assert.DoesNotContain(
            result.Items,
            item => item.Id == data.OtherTypeProduct.Id);
    }

    private Task<ReplacementCatalogData> CreateCatalogAsync()
    {
        return CatalogReaderTestDataFactory.CreateReplacementCatalogAsync(
            DbContext,
            TestContext.Current.CancellationToken);
    }
}
