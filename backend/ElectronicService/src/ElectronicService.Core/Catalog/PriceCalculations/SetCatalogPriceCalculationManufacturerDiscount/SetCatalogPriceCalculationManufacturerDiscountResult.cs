using ElectronicService.Core.Catalog.PriceCalculations.Models;

namespace ElectronicService.Core.Catalog.PriceCalculations.SetCatalogPriceCalculationManufacturerDiscount;

public sealed record SetCatalogPriceCalculationManufacturerDiscountResult(
    Guid CalculationId,
    Guid ManufacturerId,
    string ManufacturerName,
    decimal DiscountPercent,
    int AffectedLinesCount,
    decimal CalculationTotalAmount,
    IReadOnlyList<CatalogPriceCalculationRecalculatedLine> Lines);