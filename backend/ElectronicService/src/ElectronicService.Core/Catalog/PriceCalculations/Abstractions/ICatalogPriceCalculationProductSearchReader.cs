namespace ElectronicService.Core.Catalog.PriceCalculations.Abstractions;

public interface ICatalogPriceCalculationProductSearchReader
{
    Task<CatalogPriceCalculationProductsPage> SearchAsync(
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}

public sealed record CatalogPriceCalculationProductSearchItem(
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

public sealed record CatalogPriceCalculationProductsPage(
    int TotalCount,
    IReadOnlyList<CatalogPriceCalculationProductSearchItem> Items);