using ElectronicService.Domain.Catalog.PriceLists;

namespace ElectronicService.Core.Catalog.PriceLists.GetCatalogPriceListVersions;

public sealed record GetCatalogPriceListVersionsQuery(
    Guid ManufacturerId,
    Guid CurrentUserId,
    CatalogPriceListStatus? Status,
    int Page,
    int PageSize);