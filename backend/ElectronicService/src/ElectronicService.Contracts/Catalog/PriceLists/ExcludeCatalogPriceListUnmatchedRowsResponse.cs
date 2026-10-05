namespace ElectronicService.Contracts.Catalog.PriceLists;

public sealed record ExcludeCatalogPriceListUnmatchedRowsResponse(
    Guid PriceListId,
    string PriceListStatus,
    int ExcludedRowsCount,
    int RowsCount,
    int ValidRowsCount,
    int ErrorRowsCount);
