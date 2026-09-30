using ElectronicService.Domain.Catalog.PriceLists;
using ElectronicService.Infrastructure.IntegrationTests.Data;
using ElectronicService.Infrastructure.IntegrationTests.Fixtures;
using ElectronicService.Infrastructure.Postgres.Catalog.PriceCalculations;
using ElectronicService.TestCommon;

namespace ElectronicService.Infrastructure.IntegrationTests.Catalog;

[Collection(PostgreSqlIntegrationDefinition.Name)]
public sealed class CatalogPriceCalculationProductSearchReaderTests
    : PostgreSqlIntegrationTest
{
    public CatalogPriceCalculationProductSearchReaderTests(
        PostgreSqlFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task SearchAsyncReturnsProductFromActivePriceList()
    {
        var graph = PostgreSqlTestDataFactory.CreateCatalogProductGraph();
        var user = TestDataFactory.CreateTechnicalUser(
            email: $"price-search-{Guid.NewGuid():N}@example.com");

        var priceList = CatalogPriceList.Create(
            graph.Manufacturer.Id,
            user.Id,
            "prices.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [1]).Value;

        var row = CatalogPriceListRow.Create(
            priceList.Id,
            2,
            graph.Product.Article.Value,
            graph.Product.Name.Value,
            1_250m,
            null,
            null,
            "шт.").Value;

        Assert.True(row.MarkMatchedByArticle(graph.Product.Id).IsSuccess);
        Assert.True(priceList.StartProcessing().IsSuccess);
        Assert.True(priceList.CompleteProcessing(null, 1, 1, 0).IsSuccess);
        Assert.True(priceList.Activate().IsSuccess);

        await SaveGraphAsync(graph);

        DbContext.Users.Add(user);
        DbContext.CatalogPriceLists.Add(priceList);
        DbContext.CatalogPriceListRows.Add(row);

        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var reader = new CatalogPriceCalculationProductSearchReader(DbContext);

        var result = await reader.SearchAsync(
            graph.Product.Article.Value,
            skip: 0,
            take: 20,
            TestContext.Current.CancellationToken);

        var item = Assert.Single(result.Items);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(graph.Product.Id, item.ProductId);
        Assert.Equal(1_250m, item.BasePriceAmount);
    }
}
