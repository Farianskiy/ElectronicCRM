namespace ElectronicService.Contracts.Catalog.PriceLists;

public sealed record BulkUpdateCatalogPriceListRowsResponse(
    Guid PriceListId,
    string PriceListStatus,
    int RowsCount,
    int ValidRowsCount,
    int ErrorRowsCount,
    IReadOnlyList<GetCatalogPriceListRowResponse> Rows);