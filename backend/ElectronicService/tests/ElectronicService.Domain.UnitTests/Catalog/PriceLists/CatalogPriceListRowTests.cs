using ElectronicService.Domain.Catalog.PriceLists;

namespace ElectronicService.Domain.UnitTests.Catalog.PriceLists;

public sealed class CatalogPriceListRowTests
{
    [Fact]
    public void CompleteProcessingAllowsEffectiveDateToBeMissing()
    {
        var priceList = CatalogPriceList.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "prices.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [1]).Value;

        Assert.True(priceList.StartProcessing().IsSuccess);

        var result = priceList.CompleteProcessing(
            null,
            1,
            1,
            0);

        Assert.True(result.IsSuccess);
        Assert.Null(priceList.EffectiveDate);
        Assert.Equal(CatalogPriceListStatus.Ready, priceList.Status);
    }

    [Fact]
    public void CreateAllowsNameAndUnitToBeMissing()
    {
        var result = CatalogPriceListRow.Create(
            Guid.NewGuid(),
            2,
            "A-1",
            string.Empty,
            100m,
            null,
            null,
            null);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Name);
        Assert.Null(result.Value.Unit);
        Assert.Empty(result.Value.GetIssues());
    }

    [Fact]
    public void MarkDuplicateArticleAddsBlockingIssue()
    {
        var row = CatalogPriceListRow.Create(
            Guid.NewGuid(),
            2,
            "A-1",
            "Товар",
            100m,
            null,
            null,
            null).Value;

        row.MarkDuplicateArticle();

        Assert.Equal(CatalogPriceListRowStatus.Error, row.Status);
        var issue = Assert.Single(row.GetIssues());
        Assert.Equal("article.duplicate", issue.Code);
        Assert.Equal("Article", issue.Field);
    }
}
