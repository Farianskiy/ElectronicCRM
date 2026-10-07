using ElectronicService.Domain.Catalog.ProductTypes;

namespace ElectronicService.Core.Catalog.PriceCalculations.Abstractions;

public interface ICatalogPriceCalculationProductSearchReader
{
    Task<CatalogPriceCalculationProductsPage> SearchAsync(
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<CatalogPriceCalculationProductsPage> SearchAsync(
        string? search,
        int skip,
        int take,
        ProductTypeKind? productKind,
        string? productTypeCode,
        CancellationToken cancellationToken = default);
}

public sealed record CatalogPriceCalculationProductSearchItem(
    Guid ProductId,
    string ProductTypeCode,
    string ProductTypeName,
    Guid ManufacturerId,
    string ManufacturerName,
    CatalogPriceCalculationProductPriceStatus PriceStatus,
    Guid? PriceListId,
    Guid? PriceListRowId,
    DateOnly? PriceListEffectiveDate,
    string Article,
    string Name,
    string? Unit,
    decimal? BasePriceAmount,
    decimal? MrcPriceAmount);

public enum CatalogPriceCalculationProductPriceStatus
{
    Available = 0,
    ActivePriceNotFound = 1,
    ActivePriceAmbiguous = 2
}

public sealed record CatalogPriceCalculationProductsPage(
    int TotalCount,
    IReadOnlyList<CatalogPriceCalculationProductSearchItem> Items);
