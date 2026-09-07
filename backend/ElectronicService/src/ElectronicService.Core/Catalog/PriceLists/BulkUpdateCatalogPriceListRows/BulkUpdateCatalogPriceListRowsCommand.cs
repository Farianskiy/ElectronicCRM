namespace ElectronicService.Core.Catalog.PriceLists.BulkUpdateCatalogPriceListRows;

public sealed record BulkUpdateCatalogPriceListRowsCommand(
    Guid PriceListId,
    Guid CurrentUserId,
    IReadOnlyList<BulkUpdateCatalogPriceListRowItem> Rows);

public sealed record BulkUpdateCatalogPriceListRowItem(
    Guid RowId,
    string Article,
    string Name,
    decimal? BasePriceAmount,
    decimal? MrcPriceAmount,
    Uri? ProductUrl,
    string? Unit,
    Guid? ProductId);