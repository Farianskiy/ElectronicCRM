using ElectronicService.Domain.Catalog.PriceLists;

namespace ElectronicService.Core.Catalog.PriceLists.ApplyCatalogPriceListIssueGroup;

public sealed record ApplyCatalogPriceListIssueGroupResult(
    Guid PriceListId,
    CatalogPriceListStatus PriceListStatus,
    int ProcessedRowsCount,
    int RowsCount,
    int ValidRowsCount,
    int ErrorRowsCount);