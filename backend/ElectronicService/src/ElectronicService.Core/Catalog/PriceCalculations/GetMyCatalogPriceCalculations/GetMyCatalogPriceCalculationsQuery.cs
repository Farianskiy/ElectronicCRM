using ElectronicService.Domain.Catalog.PriceCalculations;

namespace ElectronicService.Core.Catalog.PriceCalculations.GetMyCatalogPriceCalculations;

public sealed record GetMyCatalogPriceCalculationsQuery(
    Guid CurrentUserId,
    CatalogPriceCalculationStatus? Status,
    int Page,
    int PageSize);