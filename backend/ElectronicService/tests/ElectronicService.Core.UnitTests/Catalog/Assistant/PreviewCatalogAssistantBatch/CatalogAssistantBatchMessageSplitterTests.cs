using ElectronicService.Core.Catalog.Assistant.PreviewCatalogAssistantBatch;

namespace ElectronicService.Core.UnitTests.Catalog.Assistant.PreviewCatalogAssistantBatch;

public sealed class CatalogAssistantBatchMessageSplitterTests
{
    [Fact]
    public void SplitKeepsSlashInsideProductName()
    {
        var result = CatalogAssistantBatchMessageSplitter.Split(
            "Корпус ВРУ 2000/600/450 2 шт");

        var line = Assert.Single(result.Lines);
        Assert.Equal("Корпус ВРУ 2000/600/450", line.SearchText);
        Assert.Equal(2m, line.Quantity);
    }

    [Fact]
    public void SplitUsesQuantityMarkersAsItemBoundaries()
    {
        var result = CatalogAssistantBatchMessageSplitter.Split(
            "Корпус ВРУ 2000/600/450 2 шт Кабельный ввод M20 10 шт");

        Assert.Collection(
            result.Lines,
            first =>
            {
                Assert.Equal("Корпус ВРУ 2000/600/450", first.SearchText);
                Assert.Equal(2m, first.Quantity);
            },
            second =>
            {
                Assert.Equal("Кабельный ввод M20", second.SearchText);
                Assert.Equal(10m, second.Quantity);
            });
    }

    [Fact]
    public void SplitStillSupportsNewLineAndSemicolonSeparators()
    {
        var result = CatalogAssistantBatchMessageSplitter.Split(
            "Автомат 1/2; Кабель 3/4\r\nКорпус ВРУ");

        Assert.Equal(3, result.Lines.Count);
        Assert.Equal(
            ["Автомат 1/2", "Кабель 3/4", "Корпус ВРУ"],
            result.Lines.Select(line => line.SearchText));
    }
}
