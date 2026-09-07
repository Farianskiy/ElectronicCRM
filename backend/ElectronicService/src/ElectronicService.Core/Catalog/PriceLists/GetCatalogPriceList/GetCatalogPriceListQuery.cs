namespace ElectronicService.Core.Catalog.PriceLists.GetCatalogPriceList;

public sealed record GetCatalogPriceListQuery(
    Guid PriceListId,
    Guid CurrentUserId);