namespace ElectronicService.Core.Catalog.PriceCalculations.SearchCatalogPriceCalculationProducts;

public sealed record SearchCatalogPriceCalculationProductsQuery(
    Guid CalculationId,
    Guid CurrentUserId,
    string? Search,
    int Page,
    int PageSize);