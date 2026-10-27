using ElectronicService.Core.Catalog.ImportBatches.PreviewExampleFill;
using ElectronicService.Core.Catalog.ImportBatches.Analysis;

namespace ElectronicService.Core.UnitTests.Catalog.ImportBatches;

public sealed class CatalogImportExampleFillTokenMatcherTests
{
    [Fact]
    public void TrainingExampleOriginKeepsDistinctProvenance()
    {
        var exampleId = Guid.NewGuid();
        var sourceRowId = Guid.NewGuid();

        var origin = CatalogImportCharacteristicValueOrigin.FromTrainingExamples(
            " C16 ",
            0.85m,
            [exampleId, exampleId],
            [sourceRowId, sourceRowId]);

        Assert.Equal(CatalogImportCharacteristicValueSource.TrainingExample, origin.Source);
        Assert.Equal("C16", origin.RawValue);
        Assert.Equal(0.85m, origin.Confidence);
        Assert.Equal([exampleId], origin.TrainingExampleIds);
        Assert.Equal([sourceRowId], origin.TrainingSourceRowIds);
    }

    [Theory]
    [InlineData("Авт. выкл. NB1-63 1P 13A 6kA х-ка B", "B")]
    [InlineData("Авт. выкл. NB1-63 1P 13A 6kA х-ка b", "B")]
    [InlineData("Дифф. автомат NXBLE-63 1P+N C16 30mA", "1P+N")]
    [InlineData("УЗО NL1-63 2P 40A 30mA тип AC", "30mA")]
    public void FindStartReturnsMatchForWholeTechnicalToken(
        string productName,
        string rawValue)
    {
        var result = CatalogImportExampleFillTokenMatcher.FindStart(
            productName,
            rawValue);

        Assert.True(result >= 0);
    }

    [Theory]
    [InlineData("Авт. выкл. NB1-63 1P 13A", "3P")]
    [InlineData("CHINT NB1-63 1P", "C")]
    [InlineData("NXBLE-63 C16", "B")]
    [InlineData("УЗО NL1-63 2P 40A", "")]
    public void FindStartDoesNotMatchSubstringInsideAnotherToken(
        string productName,
        string rawValue)
    {
        var result = CatalogImportExampleFillTokenMatcher.FindStart(
            productName,
            rawValue);

        Assert.Equal(-1, result);
    }
}
