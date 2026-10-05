namespace ElectronicService.Core.Catalog.PriceLists.ExcludeCatalogPriceListUnmatchedRows;

public sealed record ExcludeCatalogPriceListUnmatchedRowsCommand(
    Guid PriceListId,
    Guid CurrentUserId);
