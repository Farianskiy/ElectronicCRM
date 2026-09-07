namespace ElectronicService.Core.Catalog.PriceLists.ActivateCatalogPriceList;

public sealed record ActivateCatalogPriceListCommand(
    Guid PriceListId,
    Guid CurrentUserId);