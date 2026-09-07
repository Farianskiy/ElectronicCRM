namespace ElectronicService.Contracts.Catalog.PriceLists;

public sealed record ApplyCatalogPriceListIssueGroupResponse(
    Guid PriceListId,
    string PriceListStatus,
    int ProcessedRowsCount,
    int RowsCount,
    int ValidRowsCount,
    int ErrorRowsCount);