using ElectronicService.Domain.Catalog.PriceCalculations;

namespace ElectronicService.Core.Catalog.PriceCalculations.ChangeCatalogPriceCalculationLineComponentSupplyStatus;

public sealed record ChangeCatalogPriceCalculationLineComponentSupplyStatusCommand(
    Guid CalculationId,
    Guid LineId,
    Guid ComponentLineId,
    CatalogPriceCalculationLineComponentSupplyStatus SupplyStatus,
    Guid CurrentUserId);
