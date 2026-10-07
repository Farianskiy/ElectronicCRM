using ElectronicService.Domain.Catalog.PriceCalculations;

namespace ElectronicService.Core.UnitTests.Catalog.PriceCalculations;

public sealed class CatalogPriceCalculationCatalogLineTests
{
    [Fact]
    public void AddLineAcceptsCatalogProductWithoutPriceListReference()
    {
        var calculation = CatalogPriceCalculation.Create(
            Guid.CreateVersion7(),
            "Проект").Value;

        var result = calculation.AddLine(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            null,
            null,
            "A-100",
            "Товар из каталога",
            "Производитель",
            null,
            2m,
            125m,
            null);

        Assert.True(result.IsSuccess);
        var line = Assert.Single(calculation.Lines);
        Assert.Null(line.PriceListId);
        Assert.Null(line.PriceListRowId);
        Assert.Equal(250m, line.TotalAmount);
    }

    [Fact]
    public void AddLineRejectsSameCatalogProductTwice()
    {
        var calculation = CatalogPriceCalculation.Create(
            Guid.CreateVersion7(),
            "Проект").Value;
        var productId = Guid.CreateVersion7();
        var manufacturerId = Guid.CreateVersion7();

        var firstResult = calculation.AddLine(
            productId,
            manufacturerId,
            null,
            null,
            "A-100",
            "Товар из каталога",
            "Производитель",
            null,
            1m,
            125m,
            null);
        var duplicateResult = calculation.AddLine(
            productId,
            manufacturerId,
            null,
            null,
            "A-100",
            "Товар из каталога",
            "Производитель",
            null,
            1m,
            125m,
            null);

        Assert.True(firstResult.IsSuccess);
        Assert.True(duplicateResult.IsFailure);
        Assert.Equal(
            "catalog.price_calculation.line.duplicate_product",
            duplicateResult.Error.Code);
    }
}
