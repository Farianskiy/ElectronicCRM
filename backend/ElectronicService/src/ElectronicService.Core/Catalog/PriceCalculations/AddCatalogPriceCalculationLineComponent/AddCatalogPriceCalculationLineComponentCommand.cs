using ElectronicService.Domain.Catalog.PriceCalculations;

namespace ElectronicService.Core.Catalog.PriceCalculations.AddCatalogPriceCalculationLineComponent;

public sealed record AddCatalogPriceCalculationLineComponentCommand(
    Guid CalculationId,
    Guid LineId,
    Guid NeedDefinitionId,
    Guid ComponentProductId,
    int QuantityPerUnit,
    bool ManualSelection,
    CatalogPriceCalculationLineComponentSupplyStatus SupplyStatus,
    Guid CurrentUserId);
