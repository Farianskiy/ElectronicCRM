namespace ElectronicService.Core.Catalog.PriceLists.ProcessCatalogPriceList;

public sealed record ProcessCatalogPriceListCommand(
    Guid PriceListId,
    Guid CurrentUserId);