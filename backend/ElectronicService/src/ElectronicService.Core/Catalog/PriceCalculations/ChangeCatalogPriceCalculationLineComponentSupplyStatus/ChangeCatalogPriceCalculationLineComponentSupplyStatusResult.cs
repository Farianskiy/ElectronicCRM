using ElectronicService.Domain.Catalog.PriceCalculations;

namespace ElectronicService.Core.Catalog.PriceCalculations.ChangeCatalogPriceCalculationLineComponentSupplyStatus;

public sealed record ChangeCatalogPriceCalculationLineComponentSupplyStatusResult(
    Guid CalculationId,
    Guid LineId,
    Guid ComponentLineId,
    CatalogPriceCalculationLineComponentSupplyStatus SupplyStatus,
    decimal ComponentTotalAmount,
    decimal LineTotalAmount,
    decimal CalculationTotalAmount);
