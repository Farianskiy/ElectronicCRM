namespace ElectronicService.Contracts.Catalog.PriceLists;

public sealed record ProcessCatalogPriceListResponse(
    Guid PriceListId,
    int RowsCount,
    int ValidRowsCount,
    int ErrorRowsCount,
    string Status);