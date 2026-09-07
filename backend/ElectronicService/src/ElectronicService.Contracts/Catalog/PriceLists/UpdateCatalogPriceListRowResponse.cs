namespace ElectronicService.Contracts.Catalog.PriceLists;

public sealed record UpdateCatalogPriceListRowResponse(
    Guid PriceListId,
    string PriceListStatus,
    int RowsCount,
    int ValidRowsCount,
    int ErrorRowsCount,
    GetCatalogPriceListRowResponse Row);