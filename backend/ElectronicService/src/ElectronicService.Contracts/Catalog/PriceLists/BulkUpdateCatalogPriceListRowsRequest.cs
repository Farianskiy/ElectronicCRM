namespace ElectronicService.Contracts.Catalog.PriceLists;

public sealed record BulkUpdateCatalogPriceListRowsRequest(
    IReadOnlyList<BulkUpdateCatalogPriceListRowRequest>? Rows);

public sealed record BulkUpdateCatalogPriceListRowRequest(
    Guid RowId,
    string Article,
    string Name,
    decimal? BasePriceAmount,
    decimal? MrcPriceAmount,
    Uri? ProductUrl,
    string? Unit,
    Guid? ProductId);