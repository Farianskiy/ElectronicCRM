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

    [Fact]
    public void ComponentSupplyStatusControlsWhetherItAffectsProjectTotal()
    {
        var calculation = CatalogPriceCalculation.Create(
            Guid.CreateVersion7(),
            "Проект").Value;
        var lineId = calculation.AddLine(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            null,
            null,
            "MAIN-1",
            "Основной товар",
            "Производитель",
            null,
            3m,
            50m,
            null).Value;
        var componentLineId = calculation.AddLineComponent(
            lineId,
            Guid.CreateVersion7(),
            "Крепёж",
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "BOLT-1",
            "Болт",
            "Производитель",
            CatalogPriceCalculationLineComponentSelectionSource.Recommended,
            2,
            100m,
            CatalogPriceCalculationLineComponentSupplyStatus.ToPurchase).Value;

        var line = Assert.Single(calculation.Lines);
        var component = Assert.Single(line.Components);
        Assert.Equal(600m, component.TotalAmount);
        Assert.Equal(750m, calculation.TotalAmount);

        var availableResult = calculation.ChangeLineComponentSupplyStatus(
            lineId,
            componentLineId,
            CatalogPriceCalculationLineComponentSupplyStatus.AlreadyAvailable);

        Assert.True(availableResult.IsSuccess);
        Assert.Equal(0m, component.TotalAmount);
        Assert.Equal(150m, calculation.TotalAmount);

        var includedResult = calculation.ChangeLineComponentSupplyStatus(
            lineId,
            componentLineId,
            CatalogPriceCalculationLineComponentSupplyStatus.IncludedInSupply);

        Assert.True(includedResult.IsSuccess);
        Assert.Equal(0m, component.TotalAmount);
        Assert.Equal(150m, calculation.TotalAmount);

        var purchaseResult = calculation.ChangeLineComponentSupplyStatus(
            lineId,
            componentLineId,
            CatalogPriceCalculationLineComponentSupplyStatus.ToPurchase);

        Assert.True(purchaseResult.IsSuccess);
        Assert.Equal(600m, component.TotalAmount);
        Assert.Equal(750m, calculation.TotalAmount);
    }

    [Fact]
    public void NewComponentDefaultsToPurchaseStatus()
    {
        var calculation = CatalogPriceCalculation.Create(
            Guid.CreateVersion7(),
            "Проект").Value;
        var lineId = calculation.AddLine(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            null,
            null,
            "MAIN-1",
            "Основной товар",
            "Производитель",
            null,
            1m,
            50m,
            null).Value;

        calculation.AddLineComponent(
            lineId,
            Guid.CreateVersion7(),
            "Крепёж",
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "BOLT-1",
            "Болт",
            "Производитель",
            CatalogPriceCalculationLineComponentSelectionSource.Recommended,
            1,
            100m);

        var component = Assert.Single(
            Assert.Single(calculation.Lines).Components);

        Assert.Equal(
            CatalogPriceCalculationLineComponentSupplyStatus.ToPurchase,
            component.SupplyStatus);
        Assert.Equal(100m, component.TotalAmount);
    }
}
