namespace ElectronicService.Core.Catalog.PriceLists.UpdateCatalogPriceListRow;

public sealed record UpdateCatalogPriceListRowCommand(
    Guid PriceListId,
    Guid RowId,
    Guid CurrentUserId,
    string Article,
    string Name,
    decimal? BasePriceAmount,
    decimal? MrcPriceAmount,
    Uri? ProductUrl,
    string? Unit,
    Guid? ProductId);