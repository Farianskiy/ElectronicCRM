using ElectronicService.Core.Catalog.PriceCalculations.Models;

namespace ElectronicService.Core.Catalog.PriceCalculations.RemoveCatalogPriceCalculationManufacturerDiscount;

public sealed record RemoveCatalogPriceCalculationManufacturerDiscountResult(
    Guid CalculationId,
    Guid ManufacturerId,
    string ManufacturerName,
    decimal DiscountPercent,
    int AffectedLinesCount,
    decimal CalculationTotalAmount,
    IReadOnlyList<CatalogPriceCalculationRecalculatedLine> Lines);