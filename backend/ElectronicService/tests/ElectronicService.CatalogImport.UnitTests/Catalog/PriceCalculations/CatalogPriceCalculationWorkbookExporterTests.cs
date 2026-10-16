using ClosedXML.Excel;
using ElectronicService.Core.Catalog.PriceCalculations.Abstractions;
using ElectronicService.Domain.Catalog.PriceCalculations;
using ElectronicService.Infrastructure.Postgres.Catalog.PriceCalculations;

namespace ElectronicService.CatalogImport.UnitTests.Catalog.PriceCalculations;

public sealed class CatalogPriceCalculationWorkbookExporterTests
{
    [Fact]
    public void ExportWritesEditableComponentSupplyStatuses()
    {
        var exporter = new CatalogPriceCalculationWorkbookExporter();
        var calculation = CreateCalculation();

        var content = exporter.Export(calculation, DateTime.UtcNow);

        using var stream = new MemoryStream(content);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet("Комплектующие");

        Assert.Equal("Статус обеспечения", worksheet.Cell(1, 15).GetString());
        Assert.Equal("Добавить в расчёт", worksheet.Cell(2, 15).GetString());
        Assert.Equal("Уже имеется", worksheet.Cell(3, 15).GetString());
        Assert.Equal("Входит в поставку", worksheet.Cell(4, 15).GetString());
        Assert.True(worksheet.Cell(2, 15).HasDataValidation);
    }

    private static CatalogPriceCalculationDetails CreateCalculation()
    {
        var now = DateTime.UtcNow;
        var lineId = Guid.CreateVersion7();
        var components = new[]
        {
            CreateComponent(
                "CMP-1",
                CatalogPriceCalculationLineComponentSupplyStatus.ToPurchase,
                now),
            CreateComponent(
                "CMP-2",
                CatalogPriceCalculationLineComponentSupplyStatus.AlreadyAvailable,
                now),
            CreateComponent(
                "CMP-3",
                CatalogPriceCalculationLineComponentSupplyStatus.IncludedInSupply,
                now)
        };
        var line = new CatalogPriceCalculationLineDetails(
            LineId: lineId,
            ProductId: Guid.CreateVersion7(),
            ManufacturerId: Guid.CreateVersion7(),
            ManufacturerName: "IEK",
            PriceListId: null,
            PriceListRowId: null,
            Article: "MAIN-1",
            Name: "Основной товар",
            Unit: "шт.",
            Quantity: 1m,
            StockQuantity: 1m,
            ShortageQuantity: 0m,
            BasePriceAmount: 100m,
            MrcPriceAmount: null,
            DiscountPercent: 0m,
            ProjectPriceAmount: 100m,
            ProductTotalAmount: 100m,
            ComponentsTotalAmount: 10m,
            TotalAmount: 110m,
            CreatedAtUtc: now,
            UpdatedAtUtc: null,
            Components: components);

        return new CatalogPriceCalculationDetails(
            CalculationId: Guid.CreateVersion7(),
            CreatedByUserId: Guid.CreateVersion7(),
            Title: "Тестовый расчёт",
            Currency: "RUB",
            CustomerName: null,
            ObjectName: null,
            ProjectNumber: null,
            ResponsibleName: null,
            Comment: null,
            ValidUntil: null,
            Status: CatalogPriceCalculationStatus.Draft,
            TotalAmount: 110m,
            CreatedAtUtc: now,
            UpdatedAtUtc: null,
            CompletedAtUtc: null,
            ArchivedAtUtc: null,
            Lines: [line],
            ManufacturerDiscounts: []);
    }

    private static CatalogPriceCalculationLineComponentDetails CreateComponent(
        string article,
        CatalogPriceCalculationLineComponentSupplyStatus supplyStatus,
        DateTime createdAtUtc)
    {
        return new CatalogPriceCalculationLineComponentDetails(
            ComponentLineId: Guid.CreateVersion7(),
            NeedDefinitionId: Guid.CreateVersion7(),
            NeedName: "Группа комплектующих",
            ComponentProductId: Guid.CreateVersion7(),
            ManufacturerId: Guid.CreateVersion7(),
            ManufacturerName: "IEK",
            Article: article,
            Name: $"Комплектующее {article}",
            SelectionSource: CatalogPriceCalculationLineComponentSelectionSource.Manual,
            SupplyStatus: supplyStatus,
            QuantityPerUnit: 1,
            TotalQuantity: 1m,
            BasePriceAmount: 10m,
            DiscountPercent: 0m,
            ProjectPriceAmount: 10m,
            TotalAmount: supplyStatus == CatalogPriceCalculationLineComponentSupplyStatus.ToPurchase
                ? 10m
                : 0m,
            CreatedAtUtc: createdAtUtc,
            UpdatedAtUtc: null);
    }
}

