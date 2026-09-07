namespace ElectronicService.Contracts.Catalog.PriceCalculations;

public sealed record SearchCatalogPriceCalculationProductsResponse(
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyList<
        CatalogPriceCalculationProductSearchItemResponse> Items);

public sealed record CatalogPriceCalculationProductSearchItemResponse(
    Guid ProductId,
    Guid ManufacturerId,
    string ManufacturerName,
    Guid PriceListId,
    Guid PriceListRowId,
    DateOnly? PriceListEffectiveDate,
    string Article,
    string Name,
    string? Unit,
    decimal BasePriceAmount,
    decimal? MrcPriceAmount);